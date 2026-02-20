using System;
using System.Collections.Generic;
using System.Text;

namespace FastLlamaSharp.Shared.Llama
{
    public class LlamaModelEntry
    {
        public string RootDirectory { get; set; }
        public string ModelFilePath { get; set; }
        public double ModelFileSizeMb { get; set; }
        public string? MmprojFilePath { get; set; }
        public double? MmprojFileSizeMb { get; set; }
        public DateTime LastModified { get; set; }

        public string DisplayName => this.ToString();

        public LlamaModelEntry(string modelRootDirectory)
        {
            if (!Directory.Exists(modelRootDirectory))
            {
                throw new DirectoryNotFoundException($"The specified model root directory does not exist: {modelRootDirectory}");
            }

            this.RootDirectory = Path.GetFullPath(modelRootDirectory);

            string[] ggufFiles = Directory.GetFiles(this.RootDirectory, "*.gguf", SearchOption.AllDirectories);
            if (ggufFiles.Length == 0)
            {
                throw new FileNotFoundException($"No .gguf model file found in the specified directory: {this.RootDirectory}");
            }
            else if (ggufFiles.Length == 1)
            {
                this.ModelFilePath = ggufFiles[0];
                this.ModelFileSizeMb = new FileInfo(this.ModelFilePath).Length / (1024.0 * 1024.0);
            }
            else if (ggufFiles.Length == 2)
            {
                this.MmprojFilePath = ggufFiles.FirstOrDefault(f => f.Contains("mmproj", StringComparison.OrdinalIgnoreCase));
                if (this.MmprojFilePath == null)
                {
                    throw new FileNotFoundException($"Two .gguf files found but none contain 'mmproj' in the name: {string.Join(", ", ggufFiles)}");
                }
                this.MmprojFileSizeMb = new FileInfo(this.MmprojFilePath).Length / (1024.0 * 1024.0);

                this.ModelFilePath = ggufFiles.FirstOrDefault(f => !f.Contains("mmproj", StringComparison.OrdinalIgnoreCase)) ?? throw new FileNotFoundException($"Two .gguf files found but none is the main model file: {string.Join(", ", ggufFiles)}");
                this.ModelFileSizeMb = new FileInfo(this.ModelFilePath).Length / (1024.0 * 1024.0);
            }
            else
            {
                throw new FileNotFoundException($"Multiple .gguf model files found in the specified directory: {string.Join(", ", ggufFiles)}");
            }

            var lastModifiedModel = File.GetLastWriteTime(this.ModelFilePath);
            var lastModifiedMmproj = this.MmprojFilePath != null ? File.GetLastWriteTime(this.MmprojFilePath) : DateTime.MinValue;

            this.LastModified = lastModifiedModel > lastModifiedMmproj ? lastModifiedModel : lastModifiedMmproj;
        }


        public override string ToString()
        {
            string modelName = Path.GetFileNameWithoutExtension(this.ModelFilePath);
            string mmprojInfo = this.MmprojFilePath != null ? $" + mmproj ({this.MmprojFileSizeMb?.ToString("0.00")} MB)" : "";
            return $"{modelName} ({this.ModelFileSizeMb.ToString("0.00")} MB){mmprojInfo}";
        }


    }
}
