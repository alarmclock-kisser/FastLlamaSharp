using System;
using System.Collections.Generic;
using System.Text;

namespace FastLlamaSharp.Shared.Llama
{
    public class LlamaChatMessage
    {
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Role { get; set; } = string.Empty; // e.g., "User", "Assistant", "System"
        public string Content { get; set; } = string.Empty;
    }
}
