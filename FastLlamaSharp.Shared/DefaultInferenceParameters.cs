using System;
using System.Collections.Generic;
using System.Text;

namespace FastLlamaSharp.Shared
{
    public class DefaultInferenceParameters
    {
        public int MaxTokens { get; set; } = 2048;
        public float Temperature { get; set; } = 0.7f;
        public float TopP { get; set; } = 0.9f;
        public int TopK { get; set; } = 40;
        public float MinP { get; set; } = 0.0f;
        public float RepetitionPenalty { get; set; } = 1.0f;
        public float FrequencyPenalty { get; set; } = 0.0f;
        public bool Isolated { get; set; } = false;

    }
}
