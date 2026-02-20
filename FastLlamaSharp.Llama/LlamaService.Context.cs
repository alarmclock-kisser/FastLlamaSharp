using FastLlamaSharp.Shared;
using FastLlamaSharp.Shared.Llama;
using LLama;
using LLama.Abstractions;
using LLama.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace FastLlamaSharp.Llama
{
    public partial class LlamaService
    {
        private LLamaContext? _llamaContext = null;
        private ChatHistory _currentChatHistory = new();

        public string ContextsDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FastLlamaSharp", "Contexts");
        public string? CurrentlySavedContextPath { get; private set; } = null;

        public LLamaContext? GetOrCreateLlamaContext(int contextSize = 1024, int gpuLayerCount = -1, bool forceRefresh = false)
        {
            if (this.CurrentLoadedModelEntry == null)
            {
                StaticLogger.Log("No model loaded. Cannot create LLamaContext.");
                return null;
            }

            var @params = new ModelParams(this.CurrentLoadedModelEntry.ModelFilePath)
            {
                ContextSize = (uint) contextSize,
                GpuLayerCount = gpuLayerCount,
                FlashAttention = false,
                BatchSize = 128,
                UBatchSize = 128
            };

            return this.GetOrCreateLlamaContext(@params, forceRefresh);
        }

        public LLamaContext? GetOrCreateLlamaContext(ModelParams? @params = null, bool forceRefresh = false)
        {
            if (this._llamaWeights == null || this.CurrentLoadedModelEntry == null)
            {
                StaticLogger.Log("No model loaded. Cannot create LLamaContext.");
                return null;
            }

            // Wenn forceRefresh aktiv ist: Tabula Rasa
            if (forceRefresh)
            {
                StaticLogger.Log("Refreshing Context: Clearing History and KV-Cache...");

                // 1. C# Historie löschen
                this._currentChatHistory = new ChatHistory();

                // 2. Bestehende Session/Executor wegschmeißen
                this._globalExecutor = null;
                this._globalSession = null;

                // 3. Den nativen Speicher des Modells leeren
                this._llamaContext?.NativeHandle.MemoryClear();

                // Optional: Den Context komplett neu instanziieren
                this._llamaContext?.Dispose();
                this._llamaContext = null;
                this.CurrentlySavedContextPath = null;
            }

            if (this._llamaContext == null)
            {
                try
                {
                    @params ??= new ModelParams(this.CurrentLoadedModelEntry.ModelFilePath)
                    {
                        ContextSize = 1024,
                        GpuLayerCount = this._gpuLayerCount,
                        FlashAttention = false,
                        BatchSize = 128,
                        UBatchSize = 128
                    };
                    this._llamaContext = this._llamaWeights.CreateContext(@params);
                }
                catch (Exception ex)
                {
                    StaticLogger.Log($"Error creating LLamaContext: {ex.Message}");
                    return null;
                }
            }

            if (forceRefresh || this._llamaContext == null)
            {
                // ... Context laden ...
                this._llamaContext = this._llamaWeights.CreateContext(@params);

                // --- NEUE LOGIK FÜR DAS FORMAT ---
                string modelName = this.CurrentLoadedModelEntry?.DisplayName.ToLower() ?? "";
                IHistoryTransform transform = modelName.Contains("gemma")
                                              ? new GemmaHistoryTransform()
                                              : new ChatMlHistoryTransform();

                // Den Executor mit dem korrekten Transform aufbauen
                this._globalExecutor = new InteractiveExecutor(this._llamaContext);
                this._globalSession = new ChatSession(this._globalExecutor, this._currentChatHistory)
                                        .WithHistoryTransform(transform);
                // ---------------------------------
            }

            return this._llamaContext;
        }

        public LlamaContextHistory? GetContextHistory()
        {
            // 1. Prüfen, ob das Modell überhaupt geladen/aktiv ist
            if (this._llamaContext == null)
            {
                StaticLogger.Log("LLamaContext not initialized. Cannot get context history.");
                return null;
            }

            try
            {
                // 2. Neues DTO instanziieren
                var historyDto = new LlamaContextHistory
                {
                    // Eine Session-ID vergeben (z.B. neu generieren oder eine bestehende aus dem Service nehmen)
                    SessionId = Guid.NewGuid().ToString(),
                    Messages = []
                };

                // 3. Nachrichten aus der LLamaSharp ChatHistory in dein DTO mappen
                if (this._currentChatHistory != null && this._currentChatHistory.Messages != null)
                {
                    foreach (var msg in this._currentChatHistory.Messages)
                    {
                        historyDto.Messages.Add(new LlamaChatMessage
                        {
                            // Die Rolle (AuthorRole.User, System, Assistant) als String speichern
                            Role = msg.AuthorRole.ToString(),
                            Content = msg.Content
                        });
                    }
                }

                return historyDto;
            }
            catch (Exception ex)
            {
                StaticLogger.Log($"Error getting context history: {ex.Message}");
                return null;
            }
        }

        public void SaveChatHistoryToJson(string jsonFilePathOrName)
        {
            string jsonFilePath = jsonFilePathOrName;
            if (!Path.IsPathRooted(jsonFilePath))
            {
                jsonFilePath = Path.Combine(this.ContextsDirectory, jsonFilePathOrName);
            }

            try
            {
                string jsonString = JsonSerializer.Serialize(this._currentChatHistory);
                File.WriteAllText(jsonFilePath, jsonString);
                StaticLogger.Log($"Chat history saved to {jsonFilePath}");
            }
            catch (Exception ex)
            {
                StaticLogger.Log($"Error saving history: {ex.Message}");
            }
        }

        public void LoadChatHistory(string jsonFilePathOrName)
        {
            string jsonFilePath = jsonFilePathOrName;
            if (!File.Exists(jsonFilePath))
            {
                // Versucht, den Pfad im Kontext-Verzeichnis zu erstellen
                string fullPath = Path.Combine(this.ContextsDirectory, jsonFilePathOrName);
                if (File.Exists(fullPath))
                {
                    jsonFilePath = fullPath;
                }
                else
                {
                    StaticLogger.Log($"JSON history file not found: {jsonFilePathOrName}");
                    return;
                }
            }

            try
            {
                string json = File.ReadAllText(jsonFilePath);
                var messages = JsonSerializer.Deserialize<ChatHistory.Message[]>(json);

                this._currentChatHistory = new ChatHistory();
                if (messages != null)
                {
                    foreach (var msg in messages)
                    {
                        this._currentChatHistory.AddMessage(msg.AuthorRole, msg.Content);
                    }
                }
            }
            catch (Exception ex)
            {
                StaticLogger.Log($"Error loading JSON history: {ex.Message}");
            }
        }


        public void SaveContextState(string binFilePathOrName)
        {
            if (this._llamaContext == null)
            {
                return;
            }

            string binFilePath = binFilePathOrName;
            if (!File.Exists(binFilePath))
            {
                // Versucht, den Pfad im Kontext-Verzeichnis zu erstellen
                string fullPath = Path.Combine(this.ContextsDirectory, binFilePathOrName);
                Directory.CreateDirectory(this.ContextsDirectory);
                binFilePath = fullPath;
            }

            try
            {
                // Speichert den exakten internen Zustand (KV Cache)
                this._llamaContext.SaveState(binFilePath);
                StaticLogger.Log("Binary Context state successfully saved.");
            }
            catch (Exception ex)
            {
                StaticLogger.Log($"Error saving binary context state: {ex.Message}");
            }
        }

        public void LoadContextState(string binFilePathOrName, bool clearHistory = true)
        {
            if (this._llamaContext == null)
            {
                return;
            }

            string binFilePath = binFilePathOrName;
            if (!File.Exists(binFilePath))
            {
                // Versucht, den Pfad im Kontext-Verzeichnis zu erstellen
                string fullPath = Path.Combine(this.ContextsDirectory, binFilePathOrName);
                if (File.Exists(fullPath))
                {
                    binFilePath = fullPath;
                }
            }

            try
            {
                // Lädt den Zustand direkt in den RAM. Das Modell "weiß" sofort wieder alles.
                this._llamaContext.LoadState(binFilePath);
                StaticLogger.Log("Binary Context state successfully loaded.");
            }
            catch (Exception ex)
            {
                StaticLogger.Log($"Error loading binary context state: {ex.Message}");
            }
            finally
            {
                if (clearHistory)
                {
                    this._currentChatHistory = new ChatHistory();
                }
            }
        }



        public void SaveFullSession(string? folderPath = null)
        {
            if (this._llamaContext == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(folderPath) && this.CurrentlySavedContextPath != null)
            {
                folderPath = this.CurrentlySavedContextPath;
            }
            else
            {
                folderPath = folderPath ?? Path.Combine(this.ContextsDirectory, $"Session_{DateTime.Now:yyyyMMdd_HHmmss}");
            }

            try
            {
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                // 1. Binären KV-Cache des Contexts speichern
                this._llamaContext.SaveState(Path.Combine(folderPath, "context.bin"));

                // 2. Zustand des Executors (wichtig für n_past!) speichern
                this._globalExecutor ??= new InteractiveExecutor(this._llamaContext);
                this._globalExecutor.SaveState(Path.Combine(folderPath, "executor.bin"));

                // 3. Chat-Historie als JSON speichern
                string jsonPath = Path.Combine(folderPath, "history.json");
                string jsonString = JsonSerializer.Serialize(this._currentChatHistory.Messages);
                File.WriteAllText(jsonPath, jsonString);

                this.CurrentlySavedContextPath = folderPath;
                StaticLogger.Log($"Session manually saved in {folderPath}.");
            }
            catch (Exception ex)
            {
                StaticLogger.Log($"Error saving manually: {ex.Message}");
            }
        }

        public void LoadFullSession(string folderPath)
        {
            if (this._llamaContext == null)
            {
                return;
            }

            try
            {
                // 1. KV-Cache in den Context laden
                this._llamaContext.LoadState(Path.Combine(folderPath, "context.bin"));

                // 2. Executor erstellen und SEINEN Zustand laden (Synchronisiert n_past)
                this._globalExecutor = new InteractiveExecutor(this._llamaContext);
                this._globalExecutor.LoadState(Path.Combine(folderPath, "executor.bin"));

                // 3. Historie laden
                string jsonPath = Path.Combine(folderPath, "history.json");
                if (File.Exists(jsonPath))
                {
                    string json = File.ReadAllText(jsonPath);
                    var messages = JsonSerializer.Deserialize<ChatHistory.Message[]>(json);
                    this._currentChatHistory = new ChatHistory();
                    if (messages != null)
                    {
                        foreach (var m in messages)
                        {
                            this._currentChatHistory.AddMessage(m.AuthorRole, m.Content);
                        }
                    }
                }

                // 4. Session mit dem synchronisierten Executor neu aufbauen
                string modelName = this.CurrentLoadedModelEntry?.DisplayName.ToLower() ?? "";
                IHistoryTransform transform = modelName.Contains("gemma")
                                              ? new GemmaHistoryTransform()
                                              : new ChatMlHistoryTransform();

                this._globalSession = new ChatSession(this._globalExecutor, this._currentChatHistory)
                                        .WithHistoryTransform(transform);

                this.CurrentlySavedContextPath = folderPath;

                StaticLogger.Log("Session successfully loaded with synchronized context and executor state.");
            }
            catch (Exception ex)
            {
                StaticLogger.Log($"Error loading manually: {ex.Message}");
                this._globalExecutor = null;
                this._globalSession = null;
            }
        }


        public int GetCurrentTokenCount()
        {
            if (this._globalExecutor == null || this._llamaWeights == null)
            {
                return 0;
            }

            var historyText = new ChatMlHistoryTransform().HistoryToText(this._currentChatHistory);
            var tokens = this._llamaWeights.Tokenize(historyText, false, false, Encoding.UTF8);

            return tokens.Length;
        }

        public int GetCurrentContextSize()
        {
            if (this._llamaContext == null)
            {
                return 0;
            }

            return (int) this._llamaContext.ContextSize;
        }
    }
}
