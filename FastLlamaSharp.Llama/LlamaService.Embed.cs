using FastLlamaSharp.Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FastLlamaSharp.Llama
{
    public partial class LlamaService
    {
        // Dimensions for the fast CPU hashing algorithm
        private const int EmbeddingDimensions = 384;

        // INTERNAL: High-speed storage for vectors (No UI binding = No Lag)
        private readonly List<RagEntry> _internalVectorDb = new();

        // PUBLIC: The UI binds to this. One string per file
        public readonly BindingList<string> LoadedSources = new();

        public class RagEntry
        {
            public string Content { get; set; } = string.Empty;
            public float[] Vector { get; set; } = Array.Empty<float>();
            public string Source { get; set; } = string.Empty;
        }

        // --- FAST CPU EMBEDDING ---
        private static float[] Embed(string text)
        {
            var vector = new float[EmbeddingDimensions];
            if (string.IsNullOrWhiteSpace(text))
            {
                return vector;
            }

            var tokens = Tokenize(text);
            foreach (var token in tokens)
            {
                var idx = Math.Abs(token.GetHashCode()) % EmbeddingDimensions;
                vector[idx] += 1f;
            }

            Normalize(vector);
            return vector;
        }

        private static IEnumerable<string> Tokenize(string text)
        {
            var sb = new StringBuilder();
            foreach (var ch in text.ToLowerInvariant())
            {
                if (char.IsLetterOrDigit(ch))
                {
                    sb.Append(ch);
                }
                else if (sb.Length > 0)
                {
                    yield return sb.ToString();
                    sb.Clear();
                }
            }
            if (sb.Length > 0)
            {
                yield return sb.ToString();
            }
        }

        private static void Normalize(IList<float> vector)
        {
            var length = MathF.Sqrt(vector.Sum(v => v * v));
            if (length <= 0)
            {
                return;
            }

            for (var i = 0; i < vector.Count; i++)
            {
                vector[i] /= length;
            }
        }

        // --- FILE PARSERS ---

        /// <summary>
        /// Reads ANY generic JSON file and flattens it into searchable chunks.
        /// </summary>
        public async Task LoadGenericJsonAsync(string filePath)
        {
            string sourceName = Path.GetFileName(filePath);
            if (this.LoadedSources.Contains(sourceName))
            {
                return;
            }

            try
            {
                string json = await File.ReadAllTextAsync(filePath);
                this.LoadedSources.Add(sourceName);
                await StaticLogger.LogAsync($"RAG: Parsing generic JSON for {sourceName}...");

                await Task.Run(() =>
                {
                    using var doc = JsonDocument.Parse(json);
                    this.ExtractAndEmbedJsonFields(doc.RootElement, sourceName, "");
                });

                await StaticLogger.LogAsync($"RAG: Successfully embedded JSON: {sourceName}.");
            }
            catch (Exception ex)
            {
                await StaticLogger.LogAsync($"RAG Error (JSON): {ex.Message}");
                this.LoadedSources.Remove(sourceName);
            }
        }

        private void ExtractAndEmbedJsonFields(JsonElement element, string sourceName, string currentPath)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                var sb = new StringBuilder();
                if (!string.IsNullOrEmpty(currentPath))
                {
                    sb.AppendLine($"[{currentPath}]");
                }

                foreach (var prop in element.EnumerateObject())
                {
                    if (prop.Value.ValueKind == JsonValueKind.String || prop.Value.ValueKind == JsonValueKind.Number)
                    {
                        sb.AppendLine($"{prop.Name}: {prop.Value}");
                    }
                    else
                    {
                        string newPath = string.IsNullOrEmpty(currentPath) ? prop.Name : $"{currentPath}.{prop.Name}";
                        this.ExtractAndEmbedJsonFields(prop.Value, sourceName, newPath);
                    }
                }

                if (sb.Length > 0)
                {
                    string chunk = sb.ToString().TrimEnd();
                    var vector = Embed(chunk);
                    this._internalVectorDb.Add(new RagEntry { Content = chunk, Vector = vector, Source = sourceName });
                }
            }
            else if (element.ValueKind == JsonValueKind.Array)
            {
                int index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    string newPath = string.IsNullOrEmpty(currentPath) ? $"Item[{index}]" : $"{currentPath}[{index}]";
                    this.ExtractAndEmbedJsonFields(item, sourceName, newPath);
                    index++;
                }
            }
            else if (element.ValueKind == JsonValueKind.String)
            {
                string val = element.GetString() ?? "";
                if (!string.IsNullOrWhiteSpace(val))
                {
                    string chunk = string.IsNullOrEmpty(currentPath) ? val : $"{currentPath}: {val}";
                    var vector = Embed(chunk);
                    this._internalVectorDb.Add(new RagEntry { Content = chunk, Vector = vector, Source = sourceName });
                }
            }
        }

        /// <summary>
        /// Reads a standard text file and chunks it intelligently by paragraphs.
        /// </summary>
        /// <summary>
        /// Reads a standard text file and chunks it intelligently by paragraphs.
        /// Includes the filename in the text so the LLM can find it by name.
        /// </summary>
        public async Task LoadTextFileAsync(string filePath)
        {
            string sourceName = Path.GetFileName(filePath);
            if (this.LoadedSources.Contains(sourceName))
            {
                return;
            }

            try
            {
                string content = await File.ReadAllTextAsync(filePath);
                this.LoadedSources.Add(sourceName);
                await StaticLogger.LogAsync($"RAG: Parsing text file {sourceName}...");

                await Task.Run(() =>
                {
                    // Smart Chunking: Split by double newlines (paragraphs)
                    var paragraphs = content.Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (var para in paragraphs)
                    {
                        string chunkText = para.Trim();
                        if (string.IsNullOrWhiteSpace(chunkText))
                        {
                            continue;
                        }

                        // DER FIX: Wir fügen den Dateinamen SICHTBAR in den Chunk ein!
                        string searchableChunk = $"[File: {sourceName}]\n{chunkText}";

                        var vector = Embed(searchableChunk);
                        this._internalVectorDb.Add(new RagEntry { Content = searchableChunk, Vector = vector, Source = sourceName });
                    }
                });

                await StaticLogger.LogAsync($"RAG: Successfully loaded text file {sourceName}.");
            }
            catch (Exception ex)
            {
                await StaticLogger.LogAsync($"RAG Error (File): {ex.Message}");
                this.LoadedSources.Remove(sourceName);
            }
        }

        /// <summary>
        /// Searches the internal database for the most relevant snippets.
        /// </summary>
        public async Task<string> SearchRelevantContextAsync(string query, int topK = 3)
        {
            if (this._internalVectorDb.Count == 0)
            {
                return string.Empty;
            }

            var queryVector = Embed(query);
            if (queryVector.Length == 0)
            {
                return string.Empty;
            }

            var results = this._internalVectorDb
                .Select(e => new { e, Score = this.CalculateCosineSimilarity(queryVector, e.Vector) })
                .OrderByDescending(r => r.Score)
                .Take(topK)
                .ToList();

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("### RELEVANT KNOWLEDGE BASE CONTEXT ###");
            foreach (var res in results)
            {
                sb.AppendLine($"> [Source: {res.e.Source}]");
                sb.AppendLine(res.e.Content);
                sb.AppendLine();
            }
            return sb.ToString();
        }

        private float CalculateCosineSimilarity(float[] vecA, float[] vecB)
        {
            if (vecA.Length == 0 || vecB.Length == 0 || vecA.Length != vecB.Length)
            {
                return 0;
            }

            float dot = 0;
            for (int i = 0; i < vecA.Length; i++)
            {
                dot += vecA[i] * vecB[i];
            }
            return dot;
        }

        #region Management
        public void RemoveKnowledgeBySource(string sourceName)
        {
            int removed = this._internalVectorDb.RemoveAll(e => e.Source == sourceName);
            this.LoadedSources.Remove(sourceName);
            StaticLogger.Log($"Source '{sourceName}' and its {removed} entries removed from memory.");
        }

        public void RemoveKnowledgeByKeyword(string keyword)
        {
            int removedCount = this._internalVectorDb.RemoveAll(e => e.Content.Contains(keyword, StringComparison.OrdinalIgnoreCase));
            var activeSources = this._internalVectorDb.Select(e => e.Source).Distinct().ToList();
            var sourcesToRemove = this.LoadedSources.Where(s => !activeSources.Contains(s)).ToList();
            foreach (var s in sourcesToRemove)
            {
                this.LoadedSources.Remove(s);
            }

            StaticLogger.Log($"Removed {removedCount} entries containing keyword '{keyword}'.");
        }

        public void ClearKnowledgeBase()
        {
            this._internalVectorDb.Clear();
            this.LoadedSources.Clear();
            StaticLogger.Log("Knowledge base completely cleared.");
        }
        #endregion
    }
}