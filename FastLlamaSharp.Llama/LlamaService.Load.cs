using FastLlamaSharp.Shared;
using FastLlamaSharp.Shared.Llama;
using LLama;
using LLama.Common;
using LLama.Native;
using System;
using System.Collections.Generic;
using System.Text;

namespace FastLlamaSharp.Llama
{
    public partial class LlamaService
    {
        public LlamaModelEntry? CurrentLoadedModelEntry { get; private set; } = null;


        private int _gpuLayerCount = -1;

        private LLamaWeights? _llamaWeights = null;
        private MtmdWeights? _mtmdWeights = null;



        public bool LoadModel(string modelRootDirectory, int gpuLayerCount = -1, int contextSize = 1024, bool tryLoadMmproj = true)
        {
            var loadRequest = new LlamaModelLoadRequest(modelRootDirectory, gpuLayerCount, contextSize, tryLoadMmproj);
            return this.LoadModel(loadRequest);
        }

        public bool LoadModel(LlamaModelLoadRequest loadRequest)
        {
            this._gpuLayerCount = loadRequest.GpuLayerCount;

            try
            {
                ModelParams @params = new(loadRequest.ModelEntry.ModelFilePath)
                {
                    ContextSize = (uint) loadRequest.ContextSize,
                    GpuLayerCount = loadRequest.GpuLayerCount
                };

                StaticLogger.Log($"Loading model from {loadRequest.ModelEntry.ModelFilePath} with context size {@params.ContextSize} and GPU layer count {@params.GpuLayerCount}");

                // Try load llamaWeights (main gguf file)
                this._llamaWeights = LLamaWeights.LoadFromFile(@params);


                if (!string.IsNullOrEmpty(loadRequest.ModelEntry.MmprojFilePath) && loadRequest.TryLoadMmproj)
                {
                    StaticLogger.Log($"Attempting to load mmproj file from {loadRequest.ModelEntry.MmprojFilePath}");
                    try
                    {
                        MtmdContextParams mtmdParams = new()
                        {
                            UseGpu = this._gpuLayerCount > 0 || this._gpuLayerCount == -1,
                            Warmup = loadRequest.WarmupMmproj
                        };

                        this._mtmdWeights = MtmdWeights.LoadFromFile(loadRequest.ModelEntry.MmprojFilePath, this._llamaWeights, mtmdParams);
                        StaticLogger.Log($"Successfully loaded mmproj file");
                    }
                    catch (Exception ex)
                    {
                        StaticLogger.Log($"Failed to load mmproj file: {ex.Message}");
                        this._mtmdWeights = null;
                    }
                }
                else
                {
                    loadRequest.ModelEntry.MmprojFilePath = null;
                    loadRequest.WarmupMmproj = false;
                    StaticLogger.Log($"No mmproj file path provided or tryLoadMmproj is false, skipping mmproj loading");
                }

                this.CurrentLoadedModelEntry = loadRequest.ModelEntry;

                // Create context
                this.GetOrCreateLlamaContext(@params);

                return true;
            }
            catch (Exception ex)
            {
                StaticLogger.Log($"Error loading model: {ex.Message}");
                return false;
            }
        }

        public bool UnloadModel()
        {
            try
            {
                this._llamaContext?.Dispose();
                this._llamaContext = null;
                this._llamaWeights?.Dispose();
                this._llamaWeights = null;
                this._mtmdWeights?.Dispose();
                this._mtmdWeights = null;
                this.CurrentLoadedModelEntry = null;
                StaticLogger.Log("Model unloaded successfully.");
                return true;
            }
            catch (Exception ex)
            {
                StaticLogger.Log($"Error unloading model: {ex.Message}");
                return false;
            }
        }



    }
}
