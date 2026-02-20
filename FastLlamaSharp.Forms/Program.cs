using FastLlamaSharp.Llama;
using FastLlamaSharp.Shared;
using Microsoft.Extensions.Configuration;

namespace FastLlamaSharp.Forms
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var createLogFile = configuration.GetValue<bool>("CreateLogFile");
            var clearLogs = configuration.GetValue<bool>("ClearLogs");
            var modelDirectories = configuration.GetSection("ModelDirectories").Get<List<string>>();
            var gpuLayerCount = configuration.GetValue<int>("GpuLayerCount");
            var defaultLlamaModel = configuration.GetValue<string>("DefaultLlamaModel");
            var defaultContextSize = configuration.GetValue<int>("DefaultContextSize");
            var systemPrompts = configuration.GetSection("SystemPrompts").Get<List<string>>();
            var defaultInferenceParameters = configuration.GetSection("DefaultInferenceParameters").Get<DefaultInferenceParameters>() ?? new DefaultInferenceParameters();

            LlamaService llamaService = new(modelDirectories, systemPrompts, defaultInferenceParameters);

            // Init log files
            string logDirectory = Path.Combine(llamaService.ContextsDirectory, "..", "Logs");
            StaticLogger.InitializeLogFiles(logDirectory, createLogFile, clearLogs);

            ApplicationConfiguration.Initialize();
            Application.Run(new WindowMain(llamaService, defaultLlamaModel, defaultContextSize, defaultInferenceParameters));
        }
    }
}