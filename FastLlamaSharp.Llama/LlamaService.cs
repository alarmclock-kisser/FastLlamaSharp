using FastLlamaSharp.Shared;
using FastLlamaSharp.Shared.Llama;
using System;
using System.Collections.Generic;
using System.Text;

namespace FastLlamaSharp.Llama
{
    public partial class LlamaService : IDisposable
    {
        public List<string> ModelDirectories { get; set; } = [];
        public List<LlamaModelEntry> ModelsEntries { get; set; } = [];



        public LlamaService(IEnumerable<string>? additionalModelDirectories = null, IEnumerable<string>? systemPrompts = null, DefaultInferenceParameters? defaultInferenceParameters = null)
        {
            if (!Directory.Exists(this.ContextsDirectory))
            {
                Directory.CreateDirectory(this.ContextsDirectory);
            }

            LLama.Native.NativeLogConfig.llama_log_set((level, text) => {
                StaticLogger.Log($"[Native LLama]: {text.Trim()}");
            });

            if (systemPrompts != null)
            {
                this.SystemPrompt = string.Join(" ", systemPrompts.Select(p => p.TrimEnd('.') + "."));
            }

            this.SystemPrompt += this.BuildDefaultInferenceParamsSystemPrompt(defaultInferenceParameters ?? new DefaultInferenceParameters());

            this.GetModelEntries(additionalModelDirectories?.ToArray());
        }


        public string BuildDefaultInferenceParamsSystemPrompt(DefaultInferenceParameters parameters)
        {
            // Build system prompt string to inform model about inference parameters and GPU layer count
            return $"Default Inference Parameters: [" +
                   $"MaxTokens: {parameters.MaxTokens}, " +
                   $"Temperature: {parameters.Temperature}, " +
                   $"TopP: {parameters.TopP}, " +
                   $"TopK: {parameters.TopK}, " +
                   $"MinP: {parameters.MinP}, " +
                   $"RepetitionPenalty: {parameters.RepetitionPenalty}, " +
                   $"FrequencyPenalty: {parameters.FrequencyPenalty}, " +
                   $"Isolated: {parameters.Isolated}, " +
                   $"GpuLayerCount: {this._gpuLayerCount}]";
        }


        public List<LlamaModelEntry> GetModelEntries(string[]? additionalModelDirectories = null)
        {
            if (additionalModelDirectories != null)
            {
                this.ModelDirectories.AddRange(additionalModelDirectories);
            }

            this.ModelDirectories = this.ModelDirectories.Distinct().Where(dir => Directory.Exists(dir)).ToList();

            string[] subDirs = this.ModelDirectories.SelectMany(dir => Directory.GetDirectories(dir, "*", SearchOption.TopDirectoryOnly)).Where(dir => Directory.GetFiles(dir, "*.gguf").Length > 0).ToArray();

            foreach (string subDir in subDirs)
            {
                try
                {
                    this.ModelsEntries.Add(new LlamaModelEntry(subDir));
                }
                catch (Exception ex)
                {
                    StaticLogger.Log($"Error processing directory '{subDir}': {ex.Message}");
                }
            }

            return this.ModelsEntries;
        }



        public LlamaModelEntry[] GetModelsSorted(SortingOption option = SortingOption.Name)
        {
            return option switch
            {
                SortingOption.Name => this.ModelsEntries.OrderBy(m => m.DisplayName).ToArray(),
                SortingOption.Newest => this.ModelsEntries.OrderByDescending(m => m.LastModified).ToArray(),
                SortingOption.Smallest => this.ModelsEntries.OrderBy(m => m.ModelFileSizeMb).ToArray(),
                SortingOption.Vision => this.ModelsEntries.OrderByDescending(m => !string.IsNullOrEmpty(m.MmprojFilePath)).ToArray(),
                _ => this.ModelsEntries.ToArray(),
            };
        }


        public void Dispose()
        {

            GC.SuppressFinalize(this);
        }





        public enum SortingOption
        {
            Name,
            Newest,
            Smallest,
            Vision
        }
    }
}
