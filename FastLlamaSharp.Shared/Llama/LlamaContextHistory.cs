using System;
using System.Collections.Generic;
using System.Text;

namespace FastLlamaSharp.Shared.Llama
{
    public class LlamaContextHistory
    {
        public string SessionId { get; set; } = string.Empty;
        public List<LlamaChatMessage> Messages { get; set; } = [];

        // Optional: Pfad zum echten Binär-Dump des llama.cpp Contexts
        public string? ContextStateFilePath { get; set; }
    }
}
