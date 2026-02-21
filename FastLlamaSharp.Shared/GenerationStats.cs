using System;
using System.Threading;

namespace FastLlamaSharp.Shared
{
    public class GenerationStats
    {
        private int _tokensGenerated;
        private long _totalGenerationTicks;

        public int TokensGenerated => Volatile.Read(ref _tokensGenerated);

        public TimeSpan TotalGenerationTime => TimeSpan.FromTicks(Interlocked.Read(ref _totalGenerationTicks));

        public float AverageTimePerToken
        {
            get
            {
                int tokens = TokensGenerated;
                var totalSeconds = TotalGenerationTime.TotalSeconds;
                return tokens > 0 && totalSeconds > 0 ? (float)totalSeconds / tokens : 0f;
            }
        }

        public float TokensPerSecond
        {
            get
            {
                var totalSeconds = TotalGenerationTime.TotalSeconds;
                return totalSeconds > 0 ? TokensGenerated / (float)totalSeconds : 0f;
            }
        }

        public void Reset()
        {
            Interlocked.Exchange(ref _tokensGenerated, 0);
            Interlocked.Exchange(ref _totalGenerationTicks, 0);
        }

        public void IncrementToken()
        {
            Interlocked.Increment(ref _tokensGenerated);
        }

        public void UpdateElapsed(TimeSpan elapsed)
        {
            Interlocked.Exchange(ref _totalGenerationTicks, elapsed.Ticks);
        }
    }
}