using FastLlamaSharp.Llama;
using FastLlamaSharp.Shared;
using FastLlamaSharp.Shared.Llama;
using System.Text;
using static FastLlamaSharp.Llama.LlamaService;
using Timer = System.Windows.Forms.Timer;

namespace FastLlamaSharp.Forms
{
    public partial class WindowMain : Form
    {
        


        internal readonly LlamaService Llama;

        internal readonly DefaultInferenceParameters DefaultInferenceParameters;

        private CancellationTokenSource? _modelLoadingCts = null;
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

            this.ListBox_BindKnowledgeBase();
            this.ResetInferenceParameters();

            this.Load += this.WindowMain_Load;
        }




        // Methods
        private void ListBox_BindStaticLogger()
        {
            this.listBox_log.Items.Clear();
            this.listBox_llamaLog.Items.Clear();
            this.listBox_log.DataSource = null;
            this.listBox_llamaLog.DataSource = null;
            this.listBox_log.DataSource = StaticLogger.LogEntriesBindingList;
            this.listBox_llamaLog.DataSource = StaticLogger.NativeLlamaLogEntriesBindingList;
            StaticLogger.LogAdded += (s) =>
            {
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(() => this.listBox_log.TopIndex = this.listBox_log.Items.Count - 1));
                    // this.Invoke(new Action(() => this.listBox_llamaLog.TopIndex = this.listBox_llamaLog.Items.Count - 1));
                }
                else
                {
                    this.listBox_log.TopIndex = this.listBox_log.Items.Count - 1;
                    // this.listBox_llamaLog.TopIndex = this.listBox_llamaLog.Items.Count - 1;
                }
            };

            // Set StaticLogger's UI context to this form's context so it can auto-invoke when logging from other threads
            var context = SynchronizationContext.Current;
            if (context != null)
            {
                StaticLogger.SetUiContext(context);
            }
        }

        private void ListBox_BindKnowledgeBase()
        {
            this.listBox_ragEntries.Items.Clear();
            this.listBox_ragEntries.DataSource = null;
            this.listBox_ragEntries.DataSource = this.Llama.LoadedSources;
        }

        private void DomainUpDown_FillSortingOptions()
        {
            this.domainUpDown_sortOptions.Items.Clear();
            string[] options = Enum.GetNames(typeof(SortingOption));
            this.domainUpDown_sortOptions.Items.AddRange(options);
            this.domainUpDown_sortOptions.SelectedIndex = 0;
        }

        private void ComboBox_FillModelEntries(string? defaultLlamaModel = null)
        {

            var entries = this.Llama.ModelsEntries.ToArray();
            if (this.domainUpDown_sortOptions.SelectedItem is string selectedOption && Enum.TryParse<SortingOption>(selectedOption, out var option))
            {
                entries = this.Llama.GetModelsSorted(option);
            }

            this.comboBox_models.DataSource = null;
            this.comboBox_models.DataSource = entries;
            this.comboBox_models.DisplayMember = nameof(LlamaModelEntry.DisplayName);

            if (!string.IsNullOrEmpty(defaultLlamaModel))
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

            string[] tags = { "<think>", "</think>", "**", "__", "~~", "*", "_", "~", "<|im_start|>", "<|im_end|>" };
            float normalSize = this.richTextBox_conversation.Font.Size;
            float thinkSize = Math.Max(normalSize - 2f, 7f);
            string fontFamily = this.richTextBox_conversation.Font.FontFamily.Name;

            foreach (var msg in history.Messages)
            {
                if (string.IsNullOrEmpty(msg.Content))
                {
                    continue;
                }

                // 1. Cursor ans Ende setzen, um das Alignment festzulegen
                this.richTextBox_conversation.SelectionStart = this.richTextBox_conversation.TextLength;
                this.richTextBox_conversation.SelectionLength = 0;

                // 2. Alignment und Rollen-Prefix bestimmen
                bool isUser = msg.Role.Equals("User", StringComparison.OrdinalIgnoreCase);
                this.richTextBox_conversation.SelectionAlignment = isUser ? HorizontalAlignment.Right : HorizontalAlignment.Left;

                // Rollen-Name (User/Assistant) fett drucken
                this.richTextBox_conversation.SelectionColor = isUser ? this.UserMessageColor : this.AssistantMessageColor;
                this.richTextBox_conversation.SelectionFont = new Font(fontFamily, normalSize, FontStyle.Bold);
                this.richTextBox_conversation.AppendText($"{msg.Role}: ");

                // 3. Status-Variablen für das Parsing des Inhalts
                bool isThinking = false;
                bool isBold = false;
                bool isItalic = false;
                bool isStrike = false;

                // Hilfsfunktion zum formatierten Schreiben
                Action<string> appendFormatted = (textToPrint) =>
                {
                    if (string.IsNullOrEmpty(textToPrint))
                    {
                        return;
                    }

                    this.richTextBox_conversation.SelectionStart = this.richTextBox_conversation.TextLength;
                    this.richTextBox_conversation.SelectionLength = 0;

                    FontStyle currentStyle = FontStyle.Regular;
                    if (isThinking)
                    {
                        currentStyle |= FontStyle.Italic;
                    }

                    if (isBold)
                    {
                        currentStyle |= FontStyle.Bold;
                    }

                    if (isItalic)
                    {
                        currentStyle |= FontStyle.Italic;
                    }

                    if (isStrike)
                    {
                        currentStyle |= FontStyle.Strikeout;
                    }

                    this.richTextBox_conversation.SelectionFont = new Font(fontFamily, isThinking ? thinkSize : normalSize, currentStyle);
                    this.richTextBox_conversation.SelectionColor = isThinking ? Color.Gray : (isUser ? this.UserMessageColor : this.AssistantMessageColor);
                    this.richTextBox_conversation.AppendText(textToPrint);
                };

                // 4. Den kompletten Nachrichten-Inhalt parsen
                string bufferStr = msg.Content;

                while (bufferStr.Length > 0)
                {
                    int nextTagIndex = bufferStr.Length;
                    string foundTag = "";

                    // Finde das nächste Vorkommen eines bekannten Tags
                    foreach (var tag in tags)
                    {
                        int idx = bufferStr.IndexOf(tag);
                        if (idx != -1)
                        {
                            // Schutz vor Aufzählungszeichen (Bullet Points): "* Item" ist NICHT kursiv!
                            if ((tag == "*" || tag == "_") && idx + tag.Length < bufferStr.Length && char.IsWhiteSpace(bufferStr[idx + tag.Length]))
                            {
                                continue;
                            }

                            if (idx < nextTagIndex)
                            {
                                nextTagIndex = idx;
                                foundTag = tag;
                            }
                        }
                    }

                    if (nextTagIndex == bufferStr.Length)
                    {
                        // Kein Tag mehr gefunden -> den restlichen Text drucken
                        appendFormatted(bufferStr);
                        bufferStr = "";
                    }
                    else
                    {
                        // Text VOR dem gefundenen Tag drucken
                        if (nextTagIndex > 0)
                        {
                            appendFormatted(bufferStr.Substring(0, nextTagIndex));
                        }

                        // State toggeln (Tag anwenden)
                        if (foundTag == "<think>") { isThinking = true; appendFormatted("\n\n[ Thinking Process Started... ]\n"); }
                        else if (foundTag == "</think>") { isThinking = false; appendFormatted("\n[ Thinking Process Finished ]\n\n"); }
                        else if (foundTag == "**" || foundTag == "__")
                        {
                            isBold = !isBold;
                        }
                        else if (foundTag == "*" || foundTag == "_")
                        {
                            isItalic = !isItalic;
                        }
                        else if (foundTag == "~~" || foundTag == "~")
                        {
                            isStrike = !isStrike;
                        }
                        // <|im_start|> und <|im_end|> bewirken nichts, sie werden einfach geschluckt

                        // Tag aus dem Buffer entfernen, um mit dem Rest weiterzumachen
                        bufferStr = bufferStr.Substring(nextTagIndex + foundTag.Length);
                    }
                }

                // 5. Nachricht mit Zeilenumbrüchen abschließen
                this.richTextBox_conversation.SelectionFont = new Font(fontFamily, normalSize, FontStyle.Regular);
                this.richTextBox_conversation.AppendText($"{Environment.NewLine}{Environment.NewLine}");
            }

            // 6. Nach ganz unten scrollen, falls gewünscht
            if (scrollToBottom)
            {
                this.richTextBox_conversation.SelectionStart = this.richTextBox_conversation.TextLength;
                this.richTextBox_conversation.ScrollToCaret();
            }
        }

        private void Ui_UpdateState()
        {
            if (this.IsDisposed || !this.IsHandleCreated)
            {
                return; // Form ist bereits weg
            }

            if (this.InvokeRequired)
            {
                try
                {
                    // BeginInvoke wirft seltener bei Disposal; try/catch defensiv
                    this.BeginInvoke(new Action(this.Ui_UpdateState));
                }
                catch (ObjectDisposedException)
                {
                    // ignorieren
                }
                return;
            }

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
                this.button_files.Enabled = true;
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
                this.button_files.Enabled = false;
            }
        }



        // Events
        private void WindowMain_Load(object? sender, EventArgs e)
        {
            this.ResetInferenceParameters();
            this.TextBox_LoadConversationHistory();
            this.DomainUpDown_FillSortingOptions();
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

        private void listBox_llamaLog_DoubleClick(object sender, EventArgs e)
        {
            if (ModifierKeys.HasFlag(Keys.Control))
            {
                // If multiple entries, copy all selected
                var allEntries = StaticLogger.NativeLlamaLogEntriesBindingList;
                if (allEntries.Any())
                {
                    Clipboard.SetText(string.Join(Environment.NewLine, allEntries));
                }
                return;
            }

            // If single entry, copy that
            if (this.listBox_llamaLog.SelectedItem is string logEntry)
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
        private async void button_initialize_Click(object? sender, EventArgs e)
        {
            Cursor cursor = this.Cursor;
            this.Invoke(() => this.Cursor = Cursors.WaitCursor);

            if (this.Llama.CurrentLoadedModelEntry != null)
            {
                // Dispose (unload)
                try
                {
                    bool success = this.Llama.UnloadModel();
                    if (!success)
                    {
                        await StaticLogger.LogAsync("Failed to unload model.");
                    }
                    else
                    {
                        this.Invoke(() =>
                        {
                            this.comboBox_models.Enabled = true;
                            this.numericUpDown_gpuLayerCount.Enabled = true;
                            this.numericUpDown_contextSize.Enabled = true;
                            this.checkBox_tryLoadMmproj.Enabled = true;
                            this.checkBox_warmupMmproj.Enabled = true;
                            this.button_initialize.Text = "Initialize";
                            this.button_initialize.BackColor = SystemColors.Control;
                        });
                        await StaticLogger.LogAsync("Model unloaded.");
                    }
                }
                catch (Exception ex)
                {
                    await StaticLogger.LogAsync($"Error disposing model: {ex.Message}");
                }
                finally
                {
                    this.Cursor = cursor;
                }
            }
            else
            {
                var selectedModel = this.comboBox_models.SelectedItem as LlamaModelEntry;
                if (selectedModel != null)
                {
                    try
                    {
                        this.Invoke(new Action(() => this.progressBar_modelLoading.Value = 0));
                        this.Invoke(new Action(() => this.progressBar_modelLoading.Visible = true));
                        this._modelLoadingCts = new CancellationTokenSource();
                        var progress = new Progress<float>(value => this.Invoke(new Action(() => this.progressBar_modelLoading.Value = Math.Clamp((int) (value * this.progressBar_modelLoading.Maximum), 0, this.progressBar_modelLoading.Maximum))));
                        this.button_initialize.Text = "Abort";
                        this.button_initialize.Click -= this.button_initialize_Click;
                        this.button_initialize.Click += this.button_cancelLoading_Click;

                        var req = new LlamaModelLoadRequest(selectedModel.RootDirectory, (int) this.numericUpDown_gpuLayerCount.Value, (int) this.numericUpDown_contextSize.Value, this.checkBox_tryLoadMmproj.Checked, this.checkBox_warmupMmproj.Checked);
                        bool success = await this.Llama.LoadModelAsync(req, progress, this._modelLoadingCts.Token).ConfigureAwait(false);
                        if (!success)
                        {
                            await StaticLogger.LogAsync("Failed to load model.");
                        }
                        else
                        {
                            this.Invoke(new Action(() =>
                            {
                                this.comboBox_models.Enabled = false;
                                this.numericUpDown_gpuLayerCount.Enabled = false;
                                this.numericUpDown_contextSize.Enabled = false;
                                this.checkBox_tryLoadMmproj.Enabled = false;
                                this.checkBox_warmupMmproj.Enabled = false;
                                this.button_initialize.Text = "Unload";
                                this.button_initialize.BackColor = Color.LightCoral;
                            }));
                            await StaticLogger.LogAsync($"Model loaded: {selectedModel.DisplayName}");
                        }
                    }
                    catch (Exception ex)
                    {
                        await StaticLogger.LogAsync($"Error initializing model: {ex.Message}");
                    }
                    finally
                    {
                        this.Invoke(new Action(() =>
                        {
                            this.progressBar_modelLoading.Value = 0;
                            this.progressBar_modelLoading.Visible = false;
                            this.Cursor = cursor;
                        }));

                        this.button_initialize.Click -= this.button_cancelLoading_Click;
                        this.button_initialize.Click += this.button_initialize_Click;
                    }
                }
            }

            this.Ui_UpdateState();
        }

        private void button_cancelLoading_Click(object? sender, EventArgs e)
        {
            if (this._modelLoadingCts != null && !this._modelLoadingCts.IsCancellationRequested)
            {
                this._modelLoadingCts.Cancel();
                this.button_initialize.Text = "Initialize";
                this.button_initialize.Click -= this.button_cancelLoading_Click;
                this.button_initialize.Click += this.button_initialize_Click;
                StaticLogger.Log("Model loading cancellation requested.");
            }
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

            // 1. UI Update (User Message)
            this.richTextBox_conversation.SelectionStart = this.richTextBox_conversation.TextLength;
            this.richTextBox_conversation.SelectionLength = 0;
            this.richTextBox_conversation.SelectionAlignment = HorizontalAlignment.Right;
            this.richTextBox_conversation.SelectionColor = this.UserMessageColor;
            this.richTextBox_conversation.SelectionFont = new Font(this.richTextBox_conversation.Font, FontStyle.Bold);
            this.richTextBox_conversation.AppendText($"User: {prompt}{Environment.NewLine}{Environment.NewLine}");
            this.richTextBox_conversation.ScrollToCaret();

            // 2. Lock UI
            this.textBox_prompt.Enabled = false;
            this.button_send.Text = "Cancel";
            this.button_send.BackColor = Color.LightCoral;
            this.button_send.Click -= this.button_send_Click;
            this.button_send.Click += this.button_cancel_Click;

            // 3. UI Update (Assistant Start)
            this.richTextBox_conversation.SelectionStart = this.richTextBox_conversation.TextLength;
            this.richTextBox_conversation.SelectionLength = 0;
            this.richTextBox_conversation.SelectionAlignment = HorizontalAlignment.Left;
            this.richTextBox_conversation.SelectionColor = this.AssistantMessageColor;
            this.richTextBox_conversation.SelectionFont = new Font(this.richTextBox_conversation.Font, FontStyle.Bold);
            this.richTextBox_conversation.AppendText("Assistant: ");

            this._generationCts = new CancellationTokenSource();
            var token = this._generationCts.Token;

            this._generationStarted = DateTime.UtcNow;
            this._generationTimer = new Timer { Interval = 80 };
            this._generationTimer.Tick += this.Timer_generation_Tick;
            this._generationTimer.Start();

            int imageResizeMaxWidth = (int) this.numericUpDown_maxWidthPx.Value;
            bool isolated = this.checkBox_isolated.Checked;
            int maxTokens = (int) this.numericUpDown_maxTokens.Value;
            float temperature = (float) this.numericUpDown_temperature.Value;
            float topP = (float) this.numericUpDown_topP.Value;
            int topK = (int) this.numericUpDown_topK.Value;
            float minP = (float) this.numericUpDown_minP.Value;
            float repetitionPenalty = (float) this.numericUpDown_repetitionPenalty.Value;
            float frequencyPenalty = (float) this.numericUpDown_frequencyPenalty.Value;

            // --- RAG LOGIC ---
            string finalPromptForLlm = prompt;

            if (this.checkBox_rag.Checked && this.Llama.LoadedSources.Count > 0)
            {
                await StaticLogger.LogAsync("RAG: Searching knowledge base...");
                string relevantContext = await this.Llama.SearchRelevantContextAsync(prompt, 3);

                if (!string.IsNullOrWhiteSpace(relevantContext))
                {
                    finalPromptForLlm =
                        "Answer the user's question based strictly on the provided context below. " +
                        "If the context does not contain the answer, say 'I don't have information about this in my loaded documents.'\n\n" +
                        relevantContext + "\n\n" +
                        $"User Question: {prompt}";

                    await StaticLogger.LogAsync("RAG: Context injected.");
                }
                else
                {
                    finalPromptForLlm =
                        "The user is asking a question, but no relevant context was found in your documents. " +
                        "Reply exactly with: 'I don't have information about this in my loaded documents.'\n\n" +
                        $"User Question: {prompt}";

                    await StaticLogger.LogAsync("RAG: No context found. Injecting fallback prompt.");
                }
            }

            try
            {
                var responseStream = this.Llama.GenerateResponseAsync(
                    finalPromptForLlm, null, this.AttachedImages, imageResizeMaxWidth, isolated,
                    maxTokens, temperature, topP, topK, minP, repetitionPenalty, frequencyPenalty, null, token);

                int currentLine = this.richTextBox_conversation.GetLineFromCharIndex(this.richTextBox_conversation.TextLength);

                bool isThinking = false;
                bool isBold = false;
                bool isItalic = false;
                bool isStrike = false;

                float normalSize = this.richTextBox_conversation.Font.Size;
                float thinkSize = Math.Max(normalSize - 1f, 7f);
                string fontFamily = this.richTextBox_conversation.Font.FontFamily.Name;

                StringBuilder tokenBuffer = new StringBuilder();

                Action<string> appendFormatted = (textToPrint) =>
                {
                    if (string.IsNullOrEmpty(textToPrint))
                    {
                        return;
                    }

                    this.richTextBox_conversation.SelectionStart = this.richTextBox_conversation.TextLength;
                    this.richTextBox_conversation.SelectionLength = 0;

                    FontStyle currentStyle = FontStyle.Regular;
                    if (isThinking)
                    {
                        currentStyle |= FontStyle.Italic;
                    }

                    if (isBold)
                    {
                        currentStyle |= FontStyle.Bold;
                    }

                    if (isItalic)
                    {
                        currentStyle |= FontStyle.Italic;
                    }

                    if (isStrike)
                    {
                        currentStyle |= FontStyle.Strikeout;
                    }

                    this.richTextBox_conversation.SelectionFont = new Font(fontFamily, isThinking ? thinkSize : normalSize, currentStyle);
                    this.richTextBox_conversation.SelectionColor = isThinking ? Color.Gray : this.AssistantMessageColor;
                    this.richTextBox_conversation.AppendText(textToPrint);
                };

                // Alle Tags, die das System verarbeiten/verschlucken soll
                string[] tags = { "<think>", "</think>", "**", "__", "~~", "*", "_", "~", "<|im_start|>", "<|im_end|>" };

                await foreach (var textToken in responseStream)
                {
                    tokenBuffer.Append(textToken);
                    string bufferStr = tokenBuffer.ToString();

                    // 1. CLEVERE WARTE-LOGIK: Endet der Puffer mit dem *Anfang* eines beliebigen Tags?
                    bool waitingForTag = false;
                    foreach (var tag in tags)
                    {
                        // Prüfen, ob das Ende des Puffers ein unvollständiges Tag ist (z.B. "<|im_")
                        for (int i = 1; i < tag.Length; i++)
                        {
                            if (bufferStr.EndsWith(tag.Substring(0, i)))
                            {
                                waitingForTag = true;
                                break;
                            }
                        }
                        if (waitingForTag)
                        {
                            break;
                        }
                    }

                    if (waitingForTag)
                    {
                        continue; // Warten auf das nächste Stück vom Stream...
                    }

                    // 2. PUFFER VERARBEITEN
                    while (bufferStr.Length > 0)
                    {
                        int nextTagIndex = bufferStr.Length;
                        string foundTag = "";

                        foreach (var tag in tags)
                        {
                            int idx = bufferStr.IndexOf(tag);
                            if (idx != -1)
                            {
                                // Schutz vor Listen-Punkten: "* Punkt" ist nicht kursiv
                                if ((tag == "*" || tag == "_") && idx + tag.Length < bufferStr.Length && char.IsWhiteSpace(bufferStr[idx + tag.Length]))
                                {
                                    continue;
                                }

                                if (idx < nextTagIndex)
                                {
                                    nextTagIndex = idx;
                                    foundTag = tag;
                                }
                            }
                        }

                        if (nextTagIndex == bufferStr.Length)
                        {
                            appendFormatted(bufferStr);
                            bufferStr = "";
                        }
                        else
                        {
                            if (nextTagIndex > 0)
                            {
                                appendFormatted(bufferStr.Substring(0, nextTagIndex));
                            }

                            // Tag auswerten (ChatML wird hier lautlos verschluckt)
                            if (foundTag == "<think>") { isThinking = true; appendFormatted("\n\n[ Thinking Process Started... ]\n"); }
                            else if (foundTag == "</think>") { isThinking = false; appendFormatted("\n[ Thinking Process Finished ]\n\n"); }
                            else if (foundTag == "**" || foundTag == "__")
                            {
                                isBold = !isBold;
                            }
                            else if (foundTag == "*" || foundTag == "_")
                            {
                                isItalic = !isItalic;
                            }
                            else if (foundTag == "~~" || foundTag == "~")
                            {
                                isStrike = !isStrike;
                            }
                            // <|im_start|> und <|im_end|> werden einfach ignoriert!

                            bufferStr = bufferStr.Substring(nextTagIndex + foundTag.Length);
                        }
                    }

                    tokenBuffer.Clear();

                    int newLine = this.richTextBox_conversation.GetLineFromCharIndex(this.richTextBox_conversation.TextLength);
                    if (newLine > currentLine)
                    {
                        this.richTextBox_conversation.ScrollToCaret();
                        currentLine = newLine;
                    }
                }

                if (tokenBuffer.Length > 0)
                {
                    appendFormatted(tokenBuffer.ToString());
                }

                this.richTextBox_conversation.SelectionFont = new Font(this.richTextBox_conversation.Font, FontStyle.Regular);
                this.richTextBox_conversation.AppendText($"{Environment.NewLine}{Environment.NewLine}");
                this.richTextBox_conversation.ScrollToCaret();

                await StaticLogger.LogAsync("Generation finished. (" + (this.Llama.LastGenerationStats.TotalGenerationTime.TotalSeconds < 10 ? this.Llama.LastGenerationStats.TotalGenerationTime.TotalMilliseconds.ToString("N0") + "ms" : this.Llama.LastGenerationStats.TotalGenerationTime.ToString(@"mm\:ss") + ")"));
            }
            catch (OperationCanceledException)
            {
                await StaticLogger.LogAsync("Generation canceled.");
                this.richTextBox_conversation.AppendText($" [Canceled]{Environment.NewLine}{Environment.NewLine}");
                this.richTextBox_conversation.ScrollToCaret();
            }
            catch (Exception ex)
            {
                StaticLogger.Log($"Error during generation: {ex.Message}");
                this.richTextBox_conversation.AppendText($" [Error: {ex.Message}]{Environment.NewLine}{Environment.NewLine}");
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
                    this._generationTimer.Dispose();
                    this._generationTimer = null;
                }

                if (this.checkBox_autoSave.Checked && this.Llama.CurrentlySavedContextPath != null)
                {
                    try { this.Llama.SaveFullSession(this.Llama.CurrentlySavedContextPath); }
                    catch (Exception ex) { StaticLogger.Log($"Auto-save error: {ex.Message}"); }
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

        private void button_images_MouseDown(object sender, MouseEventArgs e)
        {
            // If right-clicked, remove attached images
            if (e.Button == MouseButtons.Right)
            {
                this.AttachedImages = null;
                this.label_attachedImages.Text = "Images attached: -";
                StaticLogger.Log("Attached images removed.");
            }
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



        // RAG
        private void checkBox_rag_CheckedChanged(object sender, EventArgs e)
        {
            this.numericUpDown_ragTopK.Enabled = this.checkBox_rag.Checked;
            this.label_info_ragTopK.ForeColor = this.checkBox_rag.Checked ? Color.Black : Color.Gray;
        }

        private async void button_files_Click(object sender, EventArgs e)
        {
            if (this.Llama.CurrentLoadedModelEntry == null)
            {
                await StaticLogger.LogAsync("No model loaded. Cannot process RAG files.");
                return;
            }

            using OpenFileDialog ofd = new() { Multiselect = true, Filter = "All Files|*.*|JSON|*.json|Text|*.txt;*.log|Code|*.cs;*.py;*.js;*.java" };

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                this.button_files.Enabled = false;
                this.Cursor = Cursors.WaitCursor;

                try
                {
                    foreach (var file in ofd.FileNames)
                    {
                        string extension = Path.GetExtension(file).ToLower();
                        if (extension == ".json")
                        {
                            await this.Llama.LoadGenericJsonAsync(file);
                        }
                        else
                        {
                            await this.Llama.LoadTextFileAsync(file);
                        }
                    }
                }
                finally
                {
                    this.button_files.Enabled = true;
                    this.Cursor = Cursors.Default;
                }
            }
        }

        private void listBox_ragEntries_MouseDown(object sender, MouseEventArgs e)
        {
            if (this.listBox_ragEntries.Items.Count == 0)
            {
                return; // Keine Einträge, also nichts zu tun
            }

            // If right-clicked, open context menu strip
            if (e.Button == MouseButtons.Right)
            {
                int index = this.listBox_ragEntries.IndexFromPoint(e.Location);
                if (index != ListBox.NoMatches)
                {
                    this.contextMenuStrip_rag.Show(this.listBox_ragEntries, e.Location);
                }
            }
        }

        private void removeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Get selected entry and remove from knowledge base (under mouse element if no item selected)
            int index = this.listBox_ragEntries.SelectedIndex >= 0 ? this.listBox_ragEntries.SelectedIndex : this.listBox_ragEntries.IndexFromPoint(this.listBox_ragEntries.PointToClient(Cursor.Position));
            if (index != ListBox.NoMatches)
            {
                var entry = this.listBox_ragEntries.Items[index] as string;
                if (entry != null)
                {
                    this.Llama.RemoveKnowledgeBySource(entry);
                }
            }
        }

        private void toolStripTextBox_removeKeyword_KeyDown(object sender, KeyEventArgs e)
        {
            // Enter to confirm removal of entries containing the keyword
            if (e.KeyCode == Keys.Enter)
            {
                string keyword = this.toolStripTextBox_removeKeyword.Text.Trim();
                if (!string.IsNullOrEmpty(keyword))
                {
                    this.Llama.RemoveKnowledgeByKeyword(keyword);
                    this.toolStripTextBox_removeKeyword.Clear();
                }
            }
        }

        private void clearAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Llama.ClearKnowledgeBase();
        }



        // Conversation UI settings
        private void richTextBox_conversation_MouseDown(object sender, MouseEventArgs e)
        {
            // If right-clicked, open context menu strip
            if (e.Button == MouseButtons.Right)
            {
                this.contextMenuStrip_conversation.Show(this.richTextBox_conversation, e.Location);
            }
        }

        private void setUserColorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // ColorDialog to set user message color
            ColorDialog cd = new()
            {
                AllowFullOpen = true,
                AnyColor = true,
                Color = this.UserMessageColor,
                FullOpen = true
            };

            if (cd.ShowDialog() == DialogResult.OK)
            {
                this.UserMessageColor = cd.Color;
                StaticLogger.Log($"User message color set to: {cd.Color}");
                this.TextBox_LoadConversationHistory();
            }
        }

        private void setAssistantColorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // ColorDialog to set assistant message color
            ColorDialog cd = new()
            {
                AllowFullOpen = true,
                AnyColor = true,
                Color = this.AssistantMessageColor,
                FullOpen = true
            };

            if (cd.ShowDialog() == DialogResult.OK)
            {
                this.AssistantMessageColor = cd.Color;
                StaticLogger.Log($"Assistant message color set to: {cd.Color}");
                this.TextBox_LoadConversationHistory();
            }
        }

        private void toolStripComboBox_fontSize_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.richTextBox_conversation.Font = new Font(this.richTextBox_conversation.Font.FontFamily, (float) (this.toolStripComboBox_fontSize.SelectedItem ?? 9f), this.richTextBox_conversation.Font.Style);
            this.TextBox_LoadConversationHistory();
        }

        private void domainUpDown_sortOptions_SelectedItemChanged(object sender, EventArgs e)
        {
            this.ComboBox_FillModelEntries();
        }
    }
}
