using FastLlamaSharp.Llama;
using FastLlamaSharp.Shared;
using FastLlamaSharp.Shared.Llama;
using Timer = System.Windows.Forms.Timer;

namespace FastLlamaSharp.Forms
{
    public partial class WindowMain : Form
    {
        internal readonly LlamaService Llama;

        internal readonly DefaultInferenceParameters DefaultInferenceParameters;

        private CancellationTokenSource? _generationCts = null;
        private DateTime? _generationStarted = null;
        private Timer? _generationTimer = null;

        internal string[]? AttachedImages = null;

        internal Color UserMessageColor = Color.Blue;
        internal Color AssistantMessageColor = Color.Black;


        public WindowMain(LlamaService llamaService, string? defaultLlamaModel = null, int? defaultContextSize = null, DefaultInferenceParameters? defaultInferenceParameters = null)
        {
            this.InitializeComponent();
            this.Llama = llamaService;
            this.DefaultInferenceParameters = defaultInferenceParameters ?? new DefaultInferenceParameters();

            this.ListBox_BindStaticLogger();
            this.ComboBox_FillModelEntries(defaultLlamaModel);
            if (defaultContextSize.HasValue)
            {
                this.numericUpDown_contextSize.Value = Math.Clamp(defaultContextSize.Value, this.numericUpDown_contextSize.Minimum, this.numericUpDown_contextSize.Maximum);
            }
            this.NumericUpDown_RegisterToPowOf2(this.numericUpDown_contextSize);
            this.NumericUpDown_RegisterToPowOf2(this.numericUpDown_maxTokens);

            this.Load += this.WindowMain_Load;
        }




        // Methods
        private void ListBox_BindStaticLogger()
        {
            this.listBox_log.Items.Clear();
            this.listBox_log.DataSource = null;
            this.listBox_log.DataSource = StaticLogger.LogEntriesBindingList;
            StaticLogger.LogAdded += (s) =>
            {
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(() => this.listBox_log.TopIndex = this.listBox_log.Items.Count - 1));
                }
                else
                {
                    this.listBox_log.TopIndex = this.listBox_log.Items.Count - 1;
                }
            };
        }

        private void ComboBox_FillModelEntries(string? defaultLlamaModel = null)
        {
            this.comboBox_models.DataSource = null;
            this.comboBox_models.DataSource = this.Llama.ModelsEntries;
            this.comboBox_models.DisplayMember = nameof(LlamaModelEntry.DisplayName);

            if (string.IsNullOrEmpty(defaultLlamaModel))
            {
                // Select smallest model by default
                var smallestModel = this.Llama.ModelsEntries.OrderBy(m => m.ModelFileSizeMb).FirstOrDefault();
                if (smallestModel != null)
                {
                    this.comboBox_models.SelectedItem = smallestModel;
                }
            }
            else
            {
                // Select model matching defaultLlamaModel if provided
                var matchingModel = this.Llama.ModelsEntries.FirstOrDefault(m => m.DisplayName.Contains(defaultLlamaModel, StringComparison.OrdinalIgnoreCase));
                if (matchingModel != null)
                {
                    this.comboBox_models.SelectedItem = matchingModel;
                }
            }
        }

        private void NumericUpDown_RegisterToPowOf2(NumericUpDown numericUpDown)
        {
            numericUpDown.Tag = (int) numericUpDown.Value;

            numericUpDown.ValueChanged += (s, e) =>
            {
                if (numericUpDown.Tag is int lastValue)
                {
                    int newValue = (int) numericUpDown.Value;
                    if (newValue > lastValue)
                    {
                        numericUpDown.Value = Math.Clamp(lastValue * 2, numericUpDown.Minimum, numericUpDown.Maximum);
                    }
                    else if (newValue < lastValue)
                    {
                        numericUpDown.Value = Math.Clamp(lastValue / 2, numericUpDown.Minimum, numericUpDown.Maximum);
                    }

                    numericUpDown.Tag = (int) numericUpDown.Value;
                }
            };
        }

        private void ResetInferenceParameters()
        {
            this.numericUpDown_maxTokens.Value = this.DefaultInferenceParameters.MaxTokens;
            this.numericUpDown_temperature.Value = (decimal) this.DefaultInferenceParameters.Temperature;
            this.numericUpDown_topP.Value = (decimal) this.DefaultInferenceParameters.TopP;
            this.numericUpDown_topK.Value = this.DefaultInferenceParameters.TopK;
            this.numericUpDown_minP.Value = (decimal) this.DefaultInferenceParameters.MinP;
            this.numericUpDown_repetitionPenalty.Value = (decimal) this.DefaultInferenceParameters.RepetitionPenalty;
            this.numericUpDown_frequencyPenalty.Value = (decimal) this.DefaultInferenceParameters.FrequencyPenalty;
            this.checkBox_isolated.Checked = this.DefaultInferenceParameters.Isolated;
        }

        private void TextBox_LoadConversationHistory(bool scrollToBottom = true)
        {
            this.label_currentlySavedContextPath.Text = this.Llama.CurrentlySavedContextPath != null ? $"Current context: {Path.GetFileName(this.Llama.CurrentlySavedContextPath)}" : " - Temporary Context - ";
            this.label_currentlySavedContextPath.ForeColor = this.Llama.CurrentlySavedContextPath != null ? Color.Green : Color.OrangeRed;

            var history = this.Llama.GetContextHistory();
            if (history == null || history.Messages == null)
            {
                return;
            }

            // RichTextBox vorher leeren, da wir sie komplett neu aufbauen
            this.richTextBox_conversation.Clear();

            foreach (var msg in history.Messages)
            {
                // 1. Cursor ans Ende setzen, um das Alignment für den nächsten Block festzulegen
                this.richTextBox_conversation.SelectionStart = this.richTextBox_conversation.TextLength;
                this.richTextBox_conversation.SelectionLength = 0;

                // 2. Alignment bestimmen (User = Rechts, alles andere (System/Assistant) = Links)
                bool isUser = msg.Role.Equals("User", StringComparison.OrdinalIgnoreCase);
                this.richTextBox_conversation.SelectionAlignment = isUser ? HorizontalAlignment.Right : HorizontalAlignment.Left;

                // Optional: Du kannst hier sogar die Farben ändern!
                this.richTextBox_conversation.SelectionColor = isUser ? Color.Blue : Color.Black;

                // 3. Text plus doppeltem Zeilenumbruch (für die freie Zeile) anfügen
                this.richTextBox_conversation.AppendText($"{msg.Role}: {msg.Content}{Environment.NewLine}{Environment.NewLine}");
            }

            // 4. Nach ganz unten scrollen, falls gewünscht
            if (scrollToBottom)
            {
                this.richTextBox_conversation.SelectionStart = this.richTextBox_conversation.TextLength;
                this.richTextBox_conversation.ScrollToCaret();
            }
        }

        private void Ui_UpdateState()
        {
            if (this.Llama.CurrentLoadedModelEntry != null)
            {
                int tokenCount = this.Llama.GetCurrentTokenCount();
                int maxContext = this.Llama.GetCurrentContextSize();
                this.label_tokenQuota.Text = $"({tokenCount:n0} / {maxContext:n0} Tokens)";

                this.button_send.Enabled = true;
                this.button_images.Enabled = true;
                this.button_newContext.Enabled = true;
                this.button_saveContext.Enabled = true;
                this.button_loadContext.Enabled = true;
                this.groupBox_inferenceParams.Enabled = true;
                this.checkBox_autoSave.Enabled = this.Llama.CurrentlySavedContextPath != null;
            }
            else
            {
                this.label_tokenQuota.Text = "(No model loaded)";

                this.button_send.Enabled = false;
                this.button_images.Enabled = false;
                this.button_newContext.Enabled = false;
                this.button_saveContext.Enabled = false;
                this.button_loadContext.Enabled = false;
                this.groupBox_inferenceParams.Enabled = false;
                this.checkBox_autoSave.Enabled = false;
            }
        }



        // Events
        private void WindowMain_Load(object? sender, EventArgs e)
        {
            this.ResetInferenceParameters();
            this.TextBox_LoadConversationHistory();
            this.Ui_UpdateState();

            StaticLogger.Log("Application started.");
        }

        private void listBox_log_DoubleClick(object sender, EventArgs e)
        {
            if (ModifierKeys.HasFlag(Keys.Control))
            {
                // If multiple entries, copy all selected
                var allEntries = StaticLogger.LogEntriesBindingList;
                if (allEntries.Any())
                {
                    Clipboard.SetText(string.Join(Environment.NewLine, allEntries));
                }
                return;
            }

            // If single entry, copy that
            if (this.listBox_log.SelectedItem is string logEntry)
            {
                Clipboard.SetText(logEntry);
            }
        }

        private void Timer_generation_Tick(object? sender, EventArgs e)
        {
            if (this._generationStarted.HasValue)
            {
                GenerationStats stats = this.Llama.LastGenerationStats;
                this.label_elapsed.Text = stats.TotalGenerationTime.TotalSeconds > 2 ? $"Elapsed: {stats.TotalGenerationTime:mm\\:ss}" : $"Elapsed: {stats.TotalGenerationTime.TotalMilliseconds:F1} ms";
                this.label_tokensGenerated.Text = $"Tokens: {stats.TokensGenerated}";
                this.label_tokenRate.Text = $"Tok/s: {stats.TokensPerSecond:0.##}";
            }
        }



        // Init / Dispose model
        private void button_initialize_Click(object sender, EventArgs e)
        {
            if (this.Llama.CurrentLoadedModelEntry != null)
            {
                // Dispose (unload)
                try
                {
                    bool success = this.Llama.UnloadModel();
                    if (!success)
                    {
                        StaticLogger.Log("Failed to unload model.");
                    }
                    else
                    {
                        this.comboBox_models.Enabled = true;
                        this.numericUpDown_gpuLayerCount.Enabled = true;
                        this.numericUpDown_contextSize.Enabled = true;
                        this.checkBox_tryLoadMmproj.Enabled = true;
                        this.checkBox_warmupMmproj.Enabled = true;
                        this.button_initialize.Text = "Initialize";
                        this.button_initialize.BackColor = SystemColors.Control;
                        StaticLogger.Log("Model unloaded.");
                    }
                }
                catch (Exception ex)
                {
                    StaticLogger.Log($"Error disposing model: {ex.Message}");
                }
            }
            else
            {
                var selectedModel = this.comboBox_models.SelectedItem as LlamaModelEntry;
                if (selectedModel != null)
                {
                    try
                    {
                        var req = new LlamaModelLoadRequest(selectedModel.RootDirectory, (int) this.numericUpDown_gpuLayerCount.Value, (int) this.numericUpDown_contextSize.Value, this.checkBox_tryLoadMmproj.Checked, this.checkBox_warmupMmproj.Checked);
                        bool success = this.Llama.LoadModel(req);
                        if (!success)
                        {
                            StaticLogger.Log("Failed to load model.");
                        }
                        else
                        {
                            this.comboBox_models.Enabled = false;
                            this.numericUpDown_gpuLayerCount.Enabled = false;
                            this.numericUpDown_contextSize.Enabled = false;
                            this.checkBox_tryLoadMmproj.Enabled = false;
                            this.checkBox_warmupMmproj.Enabled = false;
                            this.button_initialize.Text = "Unload";
                            this.button_initialize.BackColor = Color.LightCoral;
                            StaticLogger.Log($"Model loaded: {selectedModel.DisplayName}");
                        }
                    }
                    catch (Exception ex)
                    {
                        StaticLogger.Log($"Error initializing model: {ex.Message}");
                    }
                }
            }

            this.Ui_UpdateState();
        }

        private void checkBox_tryLoadMmproj_CheckedChanged(object sender, EventArgs e)
        {
            if (this.checkBox_tryLoadMmproj.Checked)
            {
                this.checkBox_warmupMmproj.Enabled = true;
                this.checkBox_warmupMmproj.Checked = true;
            }
            else
            {
                this.checkBox_warmupMmproj.Enabled = false;
                this.checkBox_warmupMmproj.Checked = false;
            }
        }

        private void comboBox_models_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.comboBox_models.SelectedItem is LlamaModelEntry selectedModel)
            {
                if (selectedModel.MmprojFilePath != null)
                {
                    this.checkBox_tryLoadMmproj.Enabled = true;
                    this.checkBox_tryLoadMmproj.Checked = true;
                }
                else
                {
                    this.checkBox_tryLoadMmproj.Enabled = false;
                    this.checkBox_tryLoadMmproj.Checked = false;
                }
            }
        }



        // Generate
        private void textBox_prompt_TextChanged(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(this.textBox_prompt.Text) || this.Llama.CurrentLoadedModelEntry == null)
            {
                this.button_send.Enabled = false;
            }
            else
            {
                this.button_send.Enabled = true;
            }
        }

        private async void button_send_Click(object? sender, EventArgs e)
        {
            if (this.Llama.CurrentLoadedModelEntry == null)
            {
                await StaticLogger.LogAsync("Please load a model before generating.");
                return;
            }

            string prompt = this.textBox_prompt.Text;
            if (string.IsNullOrWhiteSpace(prompt))
            {
                await StaticLogger.LogAsync("Please enter a prompt before generating.");
                return;
            }

            // 1. User-Nachricht SOFORT in der UI anzeigen (Rechtsbündig)
            this.richTextBox_conversation.SelectionStart = this.richTextBox_conversation.TextLength;
            this.richTextBox_conversation.SelectionLength = 0;
            this.richTextBox_conversation.SelectionAlignment = HorizontalAlignment.Right;
            this.richTextBox_conversation.SelectionColor = this.UserMessageColor;
            this.richTextBox_conversation.AppendText($"User: {prompt}{Environment.NewLine}{Environment.NewLine}");
            this.richTextBox_conversation.ScrollToCaret();

            // 2. Eingabefeld leeren und Button sperren
            this.textBox_prompt.Enabled = false;
            this.button_send.Text = "Cancel";
            this.button_send.BackColor = Color.LightCoral;
            this.button_send.Click -= this.button_send_Click;
            this.button_send.Click += this.button_cancel_Click;

            // 3. UI für LLM-Antwort vorbereiten (Linksbündig)
            this.richTextBox_conversation.SelectionStart = this.richTextBox_conversation.TextLength;
            this.richTextBox_conversation.SelectionLength = 0;
            this.richTextBox_conversation.SelectionAlignment = HorizontalAlignment.Left;
            this.richTextBox_conversation.SelectionColor = this.AssistantMessageColor;
            this.richTextBox_conversation.AppendText("Assistant: ");

            this._generationCts = new CancellationTokenSource();
            var token = this._generationCts.Token;
            this._generationStarted = DateTime.UtcNow;
            this._generationTimer = new Timer { Interval = 80 };
            this._generationTimer.Tick += this.Timer_generation_Tick;
            this._generationTimer.Enabled = true;
            this._generationTimer.Start();

            bool isolated = this.checkBox_isolated.Checked;
            int maxTokens = (int) this.numericUpDown_maxTokens.Value;
            float temperature = (float) this.numericUpDown_temperature.Value;
            float topP = (float) this.numericUpDown_topP.Value;
            int topK = (int) this.numericUpDown_topK.Value;
            float minP = (float) this.numericUpDown_minP.Value;
            float repetitionPenalty = (float) this.numericUpDown_repetitionPenalty.Value;
            float frequencyPenalty = (float) this.numericUpDown_frequencyPenalty.Value;

            try
            {
                // Den Stream abfragen (Hier passiert noch keine Generierung!)
                var responseStream = this.Llama.GenerateResponseAsync(
                    prompt,
                    null,
                    this.AttachedImages,
                    isolated,
                    maxTokens,
                    temperature,
                    topP,
                    topK,
                    minP,
                    repetitionPenalty,
                    frequencyPenalty,
                    null,
                    token);

                // 4. Den IAsyncEnumerable-Stream Stück für Stück auslesen
                int currentLine = this.richTextBox_conversation.GetLineFromCharIndex(this.richTextBox_conversation.TextLength);

                await foreach (var textToken in responseStream)
                {
                    // Jeden frisch generierten Schnipsel sofort in die Textbox hängen
                    // Setze sicherheitshalber Alignment und Farbe auf Assistant-Farbe
                    this.richTextBox_conversation.SelectionStart = this.richTextBox_conversation.TextLength;
                    this.richTextBox_conversation.SelectionLength = 0;
                    this.richTextBox_conversation.SelectionAlignment = HorizontalAlignment.Left;
                    this.richTextBox_conversation.SelectionColor = this.AssistantMessageColor;
                    this.richTextBox_conversation.AppendText(textToken);

                    // Prüfen, ob wir in einer neuen Zeile gelandet sind
                    int newLine = this.richTextBox_conversation.GetLineFromCharIndex(this.richTextBox_conversation.TextLength);

                    if (newLine > currentLine)
                    {
                        // Nur scrollen, wenn eine neue Zeile begonnen wurde (hart oder durch WordWrap)
                        this.richTextBox_conversation.ScrollToCaret();
                        currentLine = newLine;
                    }
                }

                // 5. Wenn er fertig ist, schließen wir die Nachricht mit Abstand ab
                this.richTextBox_conversation.AppendText($"{Environment.NewLine}{Environment.NewLine}");
                this.richTextBox_conversation.ScrollToCaret();

                await StaticLogger.LogAsync("Generation finished.");
            }
            catch (OperationCanceledException)
            {
                await StaticLogger.LogAsync("Generation canceled.");
                // Markieren, dass abgebrochen wurde
                this.richTextBox_conversation.AppendText($" [Abgebrochen]{Environment.NewLine}{Environment.NewLine}");
                this.richTextBox_conversation.ScrollToCaret();
            }
            catch (Exception ex)
            {
                StaticLogger.Log($"Error during generation: {ex.Message}");
                this.richTextBox_conversation.AppendText($" [Fehler: {ex.Message}]{Environment.NewLine}{Environment.NewLine}");
            }
            finally
            {
                this.Ui_UpdateState();

                this.textBox_prompt.Enabled = true;
                this.textBox_prompt.Clear();
                this.textBox_prompt.Focus();
                this.button_send.Text = "Send";
                this.button_send.BackColor = SystemColors.Info;
                this.button_send.Click -= this.button_cancel_Click;
                this.button_send.Click += this.button_send_Click;
                if (this._generationTimer != null)
                {
                    this._generationTimer.Stop();
                    this._generationTimer.Tick -= this.Timer_generation_Tick;
                    this._generationTimer.Dispose();
                    this._generationTimer = null;
                    this._generationStarted = null;
                }

                if (this.checkBox_autoSave.Checked && this.Llama.CurrentlySavedContextPath != null)
                {
                    try
                    {
                        this.Llama.SaveFullSession(this.Llama.CurrentlySavedContextPath);
                        StaticLogger.Log($"Auto-saved context.");
                    }
                    catch (Exception ex)
                    {
                        StaticLogger.Log($"Error auto-saving context: {ex.Message}");
                    }
                }
            }
        }

        private void button_cancel_Click(object? sender, EventArgs e)
        {
            if (this._generationCts != null && !this._generationCts.IsCancellationRequested)
            {
                this._generationCts.Cancel();
                this.button_send.Text = "Send";
            }
        }

        private void button_images_Click(object sender, EventArgs e)
        {
            // OFD for image selection at MyPictures, allowing multiple selection
            using OpenFileDialog ofd = new()
            {
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif|All Files|*.*",
                Multiselect = true
            };

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                var selectedFiles = ofd.FileNames;
                if (selectedFiles != null && selectedFiles.Length > 0)
                {
                    StaticLogger.Log($"Selected images: {string.Join(", ", selectedFiles)}");
                    this.AttachedImages = selectedFiles;
                    this.label_attachedImages.Text = this.AttachedImages.Length < 4 ? $"Images attached: [{string.Join(", ", selectedFiles.Select(f => Path.GetFileName(f)))}]" : $"Images attached: [{this.AttachedImages.Length}]";
                }
                else
                {
                    StaticLogger.Log("No images selected.");
                    this.AttachedImages = null;
                    this.label_attachedImages.Text = "Images attached: -";

                }
            }
        }

        private void button_resetParams_Click(object sender, EventArgs e)
        {
            this.ResetInferenceParameters();
        }





        // Context management (save/load)
        private void button_newContext_Click(object sender, EventArgs e)
        {
            if (this.Llama.CurrentLoadedModelEntry == null)
            {
                StaticLogger.Log("No model loaded. Cannot create or load context.");
                return;
            }

            // New context (clear conversation)
            this.Llama.GetOrCreateLlamaContext((int) this.numericUpDown_contextSize.Value, (int) this.numericUpDown_gpuLayerCount.Value, true);

            this.TextBox_LoadConversationHistory();
        }

        private void button_saveContext_Click(object sender, EventArgs e)
        {
            if (this.Llama.CurrentLoadedModelEntry == null)
            {
                StaticLogger.Log("No model loaded. Cannot save context.");
                return;
            }

            string contextName = this.Llama.CurrentlySavedContextPath ?? $"Context_{DateTime.Now:yyyyMMdd_HHmmss}";

            if (ModifierKeys.HasFlag(Keys.Control))
            {
                // Wir nutzen SaveFileDialog, weil man dort Namen TIPPSEN kann!
                using (var sfd = new SaveFileDialog())
                {
                    sfd.Title = "Enter context name or select existing folder";
                    sfd.InitialDirectory = this.Llama.ContextsDirectory;
                    // Wir geben einen Standardnamen vor
                    sfd.FileName = contextName;
                    // Wichtig: Wir wollen keine Dateiendung erzwingen
                    sfd.CheckFileExists = false;
                    sfd.OverwritePrompt = false;

                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        // sfd.FileName enthält jetzt den kompletten Pfad inklusive deinem getippten Namen
                        contextName = sfd.FileName;

                        // Da es ein Save-Dialog für DATEIEN ist, löschen wir eine evtl. 
                        // automatisch angehängte Endung, falls du eine hättest (hier nicht der Fall)
                        // Und wir erstellen den Ordner jetzt einfach:
                        try
                        {
                            if (!Directory.Exists(contextName))
                            {
                                Directory.CreateDirectory(contextName);
                            }
                        }
                        catch (Exception ex)
                        {
                            StaticLogger.Log($"Error automatically creating directory: {ex.Message}");
                        }
                    }
                    else
                    {
                        return; // User hat abgebrochen
                    }
                }
            }

            try
            {
                if (!Path.IsPathRooted(contextName))
                {
                    contextName = Path.Combine(this.Llama.ContextsDirectory, contextName);
                }

                try
                {
                    if (!Directory.Exists(contextName))
                    {
                        Directory.CreateDirectory(contextName);
                    }
                }
                catch (Exception ex)
                {
                    StaticLogger.Log($"Error creating context folder: {ex.Message}");
                }

                this.Llama.SaveFullSession(contextName);
                StaticLogger.Log($"Context saved to: {contextName}");
            }
            catch (Exception ex)
            {
                StaticLogger.Log($"Error saving context: {ex.Message}");
            }
            finally
            {
                this.TextBox_LoadConversationHistory();
            }
        }

        private void button_loadContext_Click(object sender, EventArgs e)
        {
            // FBD to select context folder with binary and json
            var fbd = new FolderBrowserDialog
            {
                Description = "Select context folder (should contain .bin and .json files)",
                InitialDirectory = this.Llama.ContextsDirectory
            };
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    this.Llama.LoadFullSession(fbd.SelectedPath);
                    StaticLogger.Log($"Context loaded from: {fbd.SelectedPath}");
                    this.checkBox_autoSave.Checked = true;
                }
                catch (Exception ex)
                {
                    StaticLogger.Log($"Error loading context: {ex.Message}");
                }
            }

            this.TextBox_LoadConversationHistory();
            this.Ui_UpdateState();
        }
    }
}
