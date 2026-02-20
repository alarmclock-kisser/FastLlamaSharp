using FastLlamaSharp.Shared;
using LLama;
using LLama.Common;
using LLama.Native;
using LLama.Sampling;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace FastLlamaSharp.Llama
{
    public partial class LlamaService
    {
        public string SystemPrompt { get; set; } = "You are a helpful assistant. Answer in the same language as the user asks.";

        public GenerationStats LastGenerationStats { get; private set; } = new GenerationStats();

        private InteractiveExecutor? _globalExecutor = null;
        private ChatSession? _globalSession = null;


        /// <summary>
        /// Generates a response from the model based on the provided prompt, optional system prompt, and optional images for multimodal models.
        /// </summary>
        /// <param name="prompt">User input.</param>
        /// <param name="systemPrompt">Optional system prompt (overrides/sets the instruction for the model).</param>
        /// <param name="images">Optional file paths to images for vision models (multimodal).</param>
        /// <param name="isolated">If true, the global history is ignored and not modified (out-of-context).</param>
        /// <param name="inferenceParams">Optional parameters for temperature, max tokens, etc.</param>
        /// <param name="cancellationToken">Token to cancel the stream.</param>
        public async IAsyncEnumerable<string> GenerateResponseAsync(
            string prompt,
            string? systemPrompt = null,
            string[]? images = null,
            bool isolated = false,
            int maxTokens = 1024,
            float temperature = 0.7f,
            float topP = 0.9f,
            int topK = 40,
            float minP = 0.0f,
            float repetitionPenalty = 1.0f,
            float frequencyPenalty = 0.0f,
            InferenceParams? inferenceParams = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (this._llamaContext == null)
            {
                StaticLogger.Log("Error: LLamaContext is not loaded. Please load a model first.");
                yield return "Error: Model is not loaded.";
                yield break;
            }

            systemPrompt ??= this.SystemPrompt;
            ChatHistory historyToUse;
            ChatSession sessionToUse;

            if (isolated)
            {
                // Isoliert: Cache leeren, neue Session
                this._llamaContext.NativeHandle.MemoryClear();
                historyToUse = new ChatHistory();
                if (!string.IsNullOrWhiteSpace(systemPrompt))
                {
                    historyToUse.AddMessage(AuthorRole.System, systemPrompt);
                }

                var tempExecutor = new InteractiveExecutor(this._llamaContext);
                sessionToUse = new ChatSession(tempExecutor, historyToUse)
                                    .WithHistoryTransform(new ChatMlHistoryTransform());

                this._globalExecutor = null;
                this._globalSession = null;
            }
            else
            {
                historyToUse = this._currentChatHistory;

                // System-Prompt logisch integrieren
                if (!string.IsNullOrWhiteSpace(systemPrompt))
                {
                    if (historyToUse.Messages.Count == 0)
                    {
                        historyToUse.AddMessage(AuthorRole.System, systemPrompt);
                    }
                    else if (historyToUse.Messages[0].AuthorRole == AuthorRole.System)
                    {
                        historyToUse.Messages[0].Content = systemPrompt;
                    }
                    else
                    {
                        historyToUse.Messages.Insert(0, new ChatHistory.Message(AuthorRole.System, systemPrompt));
                    }
                }

                // Initialisierung der Session (Falls nicht bereits durch LoadFullSession geladen)
                if (this._globalExecutor == null || this._globalSession == null)
                {
                    // Nur löschen, wenn die Historie leer ist (Neustart ohne geladene Session)
                    if (historyToUse.Messages.Count <= 1)
                    {
                        StaticLogger.Log("Fresh start: Clearing KV Cache.");
                        this._llamaContext.NativeHandle.MemoryClear();
                    }
                    else
                    {
                        StaticLogger.Log("Session state detected: Resuming without clearing cache.");
                    }

                    this._globalExecutor = new InteractiveExecutor(this._llamaContext);
                    this._globalSession = new ChatSession(this._globalExecutor, historyToUse)
                                            .WithHistoryTransform(new ChatMlHistoryTransform());
                }

                sessionToUse = this._globalSession;
            }

            // Inferenz-Parameter
            inferenceParams ??= new InferenceParams
            {
                MaxTokens = maxTokens,
                SamplingPipeline = new DefaultSamplingPipeline
                {
                    Temperature = temperature,
                    TopP = topP,
                    TopK = topK,
                    MinP = minP,
                    RepeatPenalty = repetitionPenalty,
                    FrequencyPenalty = frequencyPenalty
                },
                AntiPrompts = new List<string> { "<|im_end|>", "<|im_start|>", "User:", "user\n" }
            };

            Stopwatch sw = Stopwatch.StartNew();
            this.LastGenerationStats = new GenerationStats { TokensGenerated = 0 };

            // MTMD / Vision
            if (images != null && images.Length > 0 && this._mtmdWeights != null)
            {
                foreach (var imagePath in images)
                {
                    if (File.Exists(imagePath))
                    {
                        byte[] imageBytes = await File.ReadAllBytesAsync(imagePath, cancellationToken);
                        var embed = this._mtmdWeights.LoadMedia(imageBytes);
                        int nPast = 0;
                        this._mtmdWeights.DecodeImageChunk(
                            this._mtmdWeights.NativeHandle.DangerousGetHandle(),
                            this._llamaContext.NativeHandle,
                            embed.DangerousGetHandle(),
                            ref nPast, 0, (int) this._llamaContext.Params.BatchSize);
                    }
                }
            }

            var userMessage = new ChatHistory.Message(AuthorRole.User, prompt);

            await foreach (var token in sessionToUse.ChatAsync(userMessage, inferenceParams, cancellationToken))
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                this.LastGenerationStats.TokensGenerated++;
                this.LastGenerationStats.TotalGenerationTime = sw.Elapsed;
                yield return token;
            }

            sw.Stop();
            this.LastGenerationStats.TotalGenerationTime = sw.Elapsed;
        }



    }
}
