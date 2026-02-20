using System;
using System.Collections.Generic;
using System.Text;

namespace FastLlamaSharp.Shared.Llama
{
    public class LlamaModelLoadRequest
    {
        public LlamaModelEntry ModelEntry { get; set; }
        public int GpuLayerCount { get; set; }
        public int ContextSize { get; set; } = 1024;
        public bool TryLoadMmproj { get; set; }
        public bool WarmupMmproj { get; set; }


        public LlamaModelLoadRequest(string modelRootDirectory, int gpuLayerCount = -1, int contextSize = 1024, bool tryLoadMmproj = true, bool warmupMmproj = true)
        {
            this.ModelEntry = new LlamaModelEntry(modelRootDirectory);
            this.GpuLayerCount = gpuLayerCount;
            this.ContextSize = contextSize;
            this.TryLoadMmproj = tryLoadMmproj;
            this.WarmupMmproj = warmupMmproj;
        }






    }
}
