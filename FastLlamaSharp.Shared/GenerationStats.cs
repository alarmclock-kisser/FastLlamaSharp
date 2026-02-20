using System;
using System.Collections.Generic;
using System.Text;

namespace FastLlamaSharp.Shared
{
    public class GenerationStats
    {
        public int TokensGenerated { get; set; }
        public TimeSpan TotalGenerationTime { get; set; }
        public float AverageTimePerToken => this.TokensGenerated > 0 ? (float) this.TotalGenerationTime.TotalSeconds / this.TokensGenerated : 0;
        public float TokensPerSecond => this.TotalGenerationTime.TotalSeconds > 0 ? this.TokensGenerated / (float) this.TotalGenerationTime.TotalSeconds : 0;


    }
}
