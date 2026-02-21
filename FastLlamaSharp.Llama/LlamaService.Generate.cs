using FastLlamaSharp.Shared;
using LLama;
using LLama.Common;
using LLama.Native;
using LLama.Sampling;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
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


        private byte[] ResizeImageWithImageSharp(byte[] imageBytes, int maxWidth = 1024)
        {
            using (var image = Image.Load(imageBytes))
            {
                // Nur resizen, wenn das Bild wirklich größer als maxWidth ist
                if (image.Width <= maxWidth && image.Height <= maxWidth)
                {
                    return imageBytes;
                }

                // Proportionale Skalierung (Seitenverhältnis bleibt erhalten)
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(maxWidth, maxWidth),
                    Mode = ResizeMode.Max
                }));

                using (var ms = new MemoryStream())
                {
                    image.SaveAsJpeg(ms);
                    return ms.ToArray();
                }
            }
        }

        private byte[] ResizeImageForStability(byte[] imageBytes)
        {
            using (var image = SixLabors.ImageSharp.Image.Load(imageBytes))
            {
                // 448 ist der Sweetspot für Qwen-VL/Llava
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new SixLabors.ImageSharp.Size(448, 448),
                    Mode = ResizeMode.Max
                }));
                using (var ms = new MemoryStream())
                {
                    image.SaveAsJpeg(ms);
                    return ms.ToArray();
                }
            }
        }


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
            int imageResizeMaxWidth = 720,
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
                await StaticLogger.LogAsync("Error: LLamaContext is not loaded.");
                yield break;
            }

            systemPrompt ??= this.SystemPrompt;
            ChatHistory historyToUse;
            ChatSession sessionToUse;

            // 1. Session & Executor Logik (aus deiner stabilen Version)
            if (isolated)
            {
                this._llamaContext.NativeHandle.MemoryClear();
                historyToUse = new ChatHistory();
                if (!string.IsNullOrWhiteSpace(systemPrompt))
                {
                    historyToUse.AddMessage(AuthorRole.System, systemPrompt);
                }

                var tempExecutor = new InteractiveExecutor(this._llamaContext);
                sessionToUse = new ChatSession(tempExecutor, historyToUse).WithHistoryTransform(new ChatMlHistoryTransform());
                this._globalExecutor = null; this._globalSession = null;
            }
            else
            {
                historyToUse = this._currentChatHistory;
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

                if (this._globalExecutor == null || this._globalSession == null)
                {
                    if (historyToUse.Messages.Count <= 1)
                    {
                        this._llamaContext.NativeHandle.MemoryClear();
                    }

                    this._globalExecutor = new InteractiveExecutor(this._llamaContext);
                    this._globalSession = new ChatSession(this._globalExecutor, historyToUse)
                                            .WithHistoryTransform(new ChatMlHistoryTransform());
                }
                sessionToUse = this._globalSession;
            }

            // 2. Vision / MTMD Block mit ImageSharp Resizing
            if (images != null && images.Length > 0 && this._mtmdWeights != null)
            {
                // Wir holen den aktuellen Token-Stand als Startpunkt
                int nPast = this.GetCurrentTokenCount();

                foreach (var imagePath in images)
                {
                    if (!File.Exists(imagePath))
                    {
                        continue;
                    }

                    try
                    {
                        await StaticLogger.LogAsync($"Processing Vision: {Path.GetFileName(imagePath)}");
                        byte[] rawBytes = await File.ReadAllBytesAsync(imagePath, cancellationToken);

                        // Hier nutzen wir ImageSharp für die Optimierung
                        byte[] processedBytes = this.ResizeImageWithImageSharp(rawBytes, imageResizeMaxWidth);

                        using (var embed = this._mtmdWeights.LoadMedia(processedBytes))
                        {
                            // Bild in den KV-Cache dekodieren
                            this._mtmdWeights.DecodeImageChunk(
                                this._mtmdWeights.NativeHandle.DangerousGetHandle(),
                                this._llamaContext.NativeHandle,
                                embed.DangerousGetHandle(),
                                ref nPast, 0, (int) this._llamaContext.Params.BatchSize);
                        }
                        await StaticLogger.LogAsync($"Image successfully embedded. New context position: {nPast}");
                    }
                    catch (Exception ex)
                    {
                        await StaticLogger.LogAsync($"Vision Error: {ex.Message}");
                    }
                }
            }

            // 3. Inferenz & Streaming
            inferenceParams ??= new InferenceParams
            {
                MaxTokens = maxTokens,
                SamplingPipeline = new DefaultSamplingPipeline { Temperature = temperature, TopP = topP, TopK = topK, MinP = minP, RepeatPenalty = repetitionPenalty, FrequencyPenalty = frequencyPenalty },
                AntiPrompts = new List<string> { "<|im_end|>", "<|im_start|>", "User:", "user\n" }
            };

            Stopwatch sw = Stopwatch.StartNew();
            // Reset stats on start but keep the same instance (thread-safe updates inside GenerationStats)
            this.LastGenerationStats.Reset();
            var userMessage = new ChatHistory.Message(AuthorRole.User, prompt);

            await foreach (var token in sessionToUse.ChatAsync(userMessage, inferenceParams, cancellationToken))
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                // Use thread-safe update methods
                this.LastGenerationStats.IncrementToken();
                this.LastGenerationStats.UpdateElapsed(sw.Elapsed);
                yield return token;
            }
            sw.Stop();
        }
    }
}
