using System;
using System.Collections.Generic;
using System.Text;

namespace FastLlamaSharp.Shared.Llama
{
    public class LlamaChatRequest
    {
        public string Prompt { get; set; } = string.Empty;

        // Neu für Vision (MTMD)
        public string? ImageFilePath { get; set; }

        public int MaxTokens { get; set; } = 1024;
        public float Temperature { get; set; } = 0.7f;
        public float TopP { get; set; } = 0.9f;
        public List<string> AntiPrompts { get; set; } = ["User:", "Human:"];
    }
}
