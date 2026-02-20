namespace FastLlamaSharp.Forms
{
    partial class WindowMain
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.listBox_log = new ListBox();
            this.comboBox_models = new ComboBox();
            this.button_initialize = new Button();
            this.numericUpDown_gpuLayerCount = new NumericUpDown();
            this.label_info_gpuLayerCount = new Label();
            this.checkBox_tryLoadMmproj = new CheckBox();
            this.checkBox_warmupMmproj = new CheckBox();
            this.label_info_contextSize = new Label();
            this.numericUpDown_contextSize = new NumericUpDown();
            this.textBox_prompt = new TextBox();
            this.button_send = new Button();
            this.label_elapsed = new Label();
            this.button_images = new Button();
            this.label_attachedImages = new Label();
            this.checkBox_isolated = new CheckBox();
            this.groupBox_inferenceParams = new GroupBox();
            this.button_resetParams = new Button();
            this.label_info_maxTokens = new Label();
            this.numericUpDown_maxTokens = new NumericUpDown();
            this.label_info_frequencyPenatlty = new Label();
            this.label_info_minP = new Label();
            this.numericUpDown_minP = new NumericUpDown();
            this.numericUpDown_frequencyPenalty = new NumericUpDown();
            this.label_info_topK = new Label();
            this.numericUpDown_topK = new NumericUpDown();
            this.label_info_temperature = new Label();
            this.label_info_repeatPenalty = new Label();
            this.numericUpDown_temperature = new NumericUpDown();
            this.numericUpDown_repetitionPenalty = new NumericUpDown();
            this.label_info_topP = new Label();
            this.numericUpDown_topP = new NumericUpDown();
            this.button_newContext = new Button();
            this.button_saveContext = new Button();
            this.richTextBox_conversation = new RichTextBox();
            this.label_tokensGenerated = new Label();
            this.label_tokenRate = new Label();
            this.button_loadContext = new Button();
            this.label_currentlySavedContextPath = new Label();
            this.checkBox_autoSave = new CheckBox();
            this.label_tokenQuota = new Label();
            ((System.ComponentModel.ISupportInitialize) this.numericUpDown_gpuLayerCount).BeginInit();
            ((System.ComponentModel.ISupportInitialize) this.numericUpDown_contextSize).BeginInit();
            this.groupBox_inferenceParams.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize) this.numericUpDown_maxTokens).BeginInit();
            ((System.ComponentModel.ISupportInitialize) this.numericUpDown_minP).BeginInit();
            ((System.ComponentModel.ISupportInitialize) this.numericUpDown_frequencyPenalty).BeginInit();
            ((System.ComponentModel.ISupportInitialize) this.numericUpDown_topK).BeginInit();
            ((System.ComponentModel.ISupportInitialize) this.numericUpDown_temperature).BeginInit();
            ((System.ComponentModel.ISupportInitialize) this.numericUpDown_repetitionPenalty).BeginInit();
            ((System.ComponentModel.ISupportInitialize) this.numericUpDown_topP).BeginInit();
            this.SuspendLayout();
            // 
            // listBox_log
            // 
            this.listBox_log.Font = new Font("Segoe UI", 8.25F, FontStyle.Regular, GraphicsUnit.Point,  0);
            this.listBox_log.FormattingEnabled = true;
            this.listBox_log.HorizontalScrollbar = true;
            this.listBox_log.Location = new Point(1052, 12);
            this.listBox_log.Name = "listBox_log";
            this.listBox_log.Size = new Size(360, 277);
            this.listBox_log.TabIndex = 0;
            this.listBox_log.DoubleClick += this.listBox_log_DoubleClick;
            // 
            // comboBox_models
            // 
            this.comboBox_models.FormattingEnabled = true;
            this.comboBox_models.Location = new Point(108, 12);
            this.comboBox_models.Name = "comboBox_models";
            this.comboBox_models.Size = new Size(540, 23);
            this.comboBox_models.TabIndex = 1;
            this.comboBox_models.Text = "Select a GGUF model ...";
            this.comboBox_models.SelectedIndexChanged += this.comboBox_models_SelectedIndexChanged;
            // 
            // button_initialize
            // 
            this.button_initialize.Location = new Point(12, 12);
            this.button_initialize.Name = "button_initialize";
            this.button_initialize.Size = new Size(90, 23);
            this.button_initialize.TabIndex = 2;
            this.button_initialize.Text = "Initialize";
            this.button_initialize.UseVisualStyleBackColor = true;
            this.button_initialize.Click += this.button_initialize_Click;
            // 
            // numericUpDown_gpuLayerCount
            // 
            this.numericUpDown_gpuLayerCount.Location = new Point(12, 56);
            this.numericUpDown_gpuLayerCount.Maximum = new decimal(new int[] { 999, 0, 0, 0 });
            this.numericUpDown_gpuLayerCount.Minimum = new decimal(new int[] { 1, 0, 0, int.MinValue });
            this.numericUpDown_gpuLayerCount.Name = "numericUpDown_gpuLayerCount";
            this.numericUpDown_gpuLayerCount.Size = new Size(90, 23);
            this.numericUpDown_gpuLayerCount.TabIndex = 3;
            this.numericUpDown_gpuLayerCount.Value = new decimal(new int[] { 1, 0, 0, int.MinValue });
            // 
            // label_info_gpuLayerCount
            // 
            this.label_info_gpuLayerCount.AutoSize = true;
            this.label_info_gpuLayerCount.Location = new Point(12, 38);
            this.label_info_gpuLayerCount.Name = "label_info_gpuLayerCount";
            this.label_info_gpuLayerCount.Size = new Size(92, 15);
            this.label_info_gpuLayerCount.TabIndex = 4;
            this.label_info_gpuLayerCount.Text = "GPU layer count";
            // 
            // checkBox_tryLoadMmproj
            // 
            this.checkBox_tryLoadMmproj.AutoSize = true;
            this.checkBox_tryLoadMmproj.Checked = true;
            this.checkBox_tryLoadMmproj.CheckState = CheckState.Checked;
            this.checkBox_tryLoadMmproj.Location = new Point(10, 129);
            this.checkBox_tryLoadMmproj.Name = "checkBox_tryLoadMmproj";
            this.checkBox_tryLoadMmproj.Size = new Size(86, 19);
            this.checkBox_tryLoadMmproj.TabIndex = 5;
            this.checkBox_tryLoadMmproj.Text = "+ MMPROJ";
            this.checkBox_tryLoadMmproj.UseVisualStyleBackColor = true;
            this.checkBox_tryLoadMmproj.CheckedChanged += this.checkBox_tryLoadMmproj_CheckedChanged;
            // 
            // checkBox_warmupMmproj
            // 
            this.checkBox_warmupMmproj.AutoSize = true;
            this.checkBox_warmupMmproj.Checked = true;
            this.checkBox_warmupMmproj.CheckState = CheckState.Checked;
            this.checkBox_warmupMmproj.Location = new Point(10, 154);
            this.checkBox_warmupMmproj.Name = "checkBox_warmupMmproj";
            this.checkBox_warmupMmproj.Size = new Size(72, 19);
            this.checkBox_warmupMmproj.TabIndex = 6;
            this.checkBox_warmupMmproj.Text = "Warmup";
            this.checkBox_warmupMmproj.UseVisualStyleBackColor = true;
            // 
            // label_info_contextSize
            // 
            this.label_info_contextSize.AutoSize = true;
            this.label_info_contextSize.Location = new Point(10, 82);
            this.label_info_contextSize.Name = "label_info_contextSize";
            this.label_info_contextSize.Size = new Size(70, 15);
            this.label_info_contextSize.TabIndex = 8;
            this.label_info_contextSize.Text = "Context size";
            // 
            // numericUpDown_contextSize
            // 
            this.numericUpDown_contextSize.Location = new Point(10, 100);
            this.numericUpDown_contextSize.Maximum = new decimal(new int[] { 262144, 0, 0, 0 });
            this.numericUpDown_contextSize.Minimum = new decimal(new int[] { 128, 0, 0, 0 });
            this.numericUpDown_contextSize.Name = "numericUpDown_contextSize";
            this.numericUpDown_contextSize.Size = new Size(90, 23);
            this.numericUpDown_contextSize.TabIndex = 7;
            this.numericUpDown_contextSize.Value = new decimal(new int[] { 2048, 0, 0, 0 });
            // 
            // textBox_prompt
            // 
            this.textBox_prompt.Location = new Point(12, 571);
            this.textBox_prompt.MaxLength = 262144;
            this.textBox_prompt.Multiline = true;
            this.textBox_prompt.Name = "textBox_prompt";
            this.textBox_prompt.PlaceholderText = "Enter a prompt and send it to the LLM ...";
            this.textBox_prompt.Size = new Size(636, 98);
            this.textBox_prompt.TabIndex = 9;
            this.textBox_prompt.TextChanged += this.textBox_prompt_TextChanged;
            // 
            // button_send
            // 
            this.button_send.BackColor = SystemColors.Info;
            this.button_send.Location = new Point(654, 646);
            this.button_send.Name = "button_send";
            this.button_send.Size = new Size(85, 23);
            this.button_send.TabIndex = 10;
            this.button_send.Text = "Send";
            this.button_send.UseVisualStyleBackColor = false;
            this.button_send.Click += this.button_send_Click;
            // 
            // label_elapsed
            // 
            this.label_elapsed.AutoSize = true;
            this.label_elapsed.Location = new Point(12, 516);
            this.label_elapsed.Name = "label_elapsed";
            this.label_elapsed.Size = new Size(71, 15);
            this.label_elapsed.TabIndex = 11;
            this.label_elapsed.Text = "Elapsed: -:--";
            // 
            // button_images
            // 
            this.button_images.Location = new Point(654, 619);
            this.button_images.Name = "button_images";
            this.button_images.Size = new Size(85, 23);
            this.button_images.TabIndex = 13;
            this.button_images.Text = "Images";
            this.button_images.UseVisualStyleBackColor = true;
            this.button_images.Click += this.button_images_Click;
            // 
            // label_attachedImages
            // 
            this.label_attachedImages.AutoSize = true;
            this.label_attachedImages.Location = new Point(12, 553);
            this.label_attachedImages.Name = "label_attachedImages";
            this.label_attachedImages.Size = new Size(105, 15);
            this.label_attachedImages.TabIndex = 14;
            this.label_attachedImages.Text = "Images attached: -";
            // 
            // checkBox_isolated
            // 
            this.checkBox_isolated.AutoSize = true;
            this.checkBox_isolated.Location = new Point(6, 128);
            this.checkBox_isolated.Name = "checkBox_isolated";
            this.checkBox_isolated.Size = new Size(67, 19);
            this.checkBox_isolated.TabIndex = 15;
            this.checkBox_isolated.Text = "Isolated";
            this.checkBox_isolated.UseVisualStyleBackColor = true;
            // 
            // groupBox_inferenceParams
            // 
            this.groupBox_inferenceParams.Controls.Add(this.button_resetParams);
            this.groupBox_inferenceParams.Controls.Add(this.checkBox_isolated);
            this.groupBox_inferenceParams.Controls.Add(this.label_info_maxTokens);
            this.groupBox_inferenceParams.Controls.Add(this.numericUpDown_maxTokens);
            this.groupBox_inferenceParams.Controls.Add(this.label_info_frequencyPenatlty);
            this.groupBox_inferenceParams.Controls.Add(this.label_info_minP);
            this.groupBox_inferenceParams.Controls.Add(this.numericUpDown_minP);
            this.groupBox_inferenceParams.Controls.Add(this.numericUpDown_frequencyPenalty);
            this.groupBox_inferenceParams.Controls.Add(this.label_info_topK);
            this.groupBox_inferenceParams.Controls.Add(this.numericUpDown_topK);
            this.groupBox_inferenceParams.Controls.Add(this.label_info_temperature);
            this.groupBox_inferenceParams.Controls.Add(this.label_info_repeatPenalty);
            this.groupBox_inferenceParams.Controls.Add(this.numericUpDown_temperature);
            this.groupBox_inferenceParams.Controls.Add(this.numericUpDown_repetitionPenalty);
            this.groupBox_inferenceParams.Controls.Add(this.label_info_topP);
            this.groupBox_inferenceParams.Controls.Add(this.numericUpDown_topP);
            this.groupBox_inferenceParams.Location = new Point(745, 512);
            this.groupBox_inferenceParams.Name = "groupBox_inferenceParams";
            this.groupBox_inferenceParams.Size = new Size(309, 157);
            this.groupBox_inferenceParams.TabIndex = 16;
            this.groupBox_inferenceParams.TabStop = false;
            this.groupBox_inferenceParams.Text = "Inference Paramters";
            // 
            // button_resetParams
            // 
            this.button_resetParams.Location = new Point(228, 128);
            this.button_resetParams.Name = "button_resetParams";
            this.button_resetParams.Size = new Size(75, 23);
            this.button_resetParams.TabIndex = 17;
            this.button_resetParams.Text = "Reset";
            this.button_resetParams.UseVisualStyleBackColor = true;
            this.button_resetParams.Click += this.button_resetParams_Click;
            // 
            // label_info_maxTokens
            // 
            this.label_info_maxTokens.AutoSize = true;
            this.label_info_maxTokens.Location = new Point(6, 19);
            this.label_info_maxTokens.Name = "label_info_maxTokens";
            this.label_info_maxTokens.Size = new Size(67, 15);
            this.label_info_maxTokens.TabIndex = 30;
            this.label_info_maxTokens.Text = "Max tokens";
            // 
            // numericUpDown_maxTokens
            // 
            this.numericUpDown_maxTokens.Location = new Point(6, 37);
            this.numericUpDown_maxTokens.Maximum = new decimal(new int[] { 8192, 0, 0, 0 });
            this.numericUpDown_maxTokens.Minimum = new decimal(new int[] { 64, 0, 0, 0 });
            this.numericUpDown_maxTokens.Name = "numericUpDown_maxTokens";
            this.numericUpDown_maxTokens.Size = new Size(60, 23);
            this.numericUpDown_maxTokens.TabIndex = 29;
            this.numericUpDown_maxTokens.Value = new decimal(new int[] { 512, 0, 0, 0 });
            // 
            // label_info_frequencyPenatlty
            // 
            this.label_info_frequencyPenatlty.AutoSize = true;
            this.label_info_frequencyPenatlty.Location = new Point(193, 63);
            this.label_info_frequencyPenatlty.Name = "label_info_frequencyPenatlty";
            this.label_info_frequencyPenatlty.Size = new Size(104, 15);
            this.label_info_frequencyPenatlty.TabIndex = 28;
            this.label_info_frequencyPenatlty.Text = "Frequency Penalty";
            // 
            // label_info_minP
            // 
            this.label_info_minP.AutoSize = true;
            this.label_info_minP.Location = new Point(101, 19);
            this.label_info_minP.Name = "label_info_minP";
            this.label_info_minP.Size = new Size(38, 15);
            this.label_info_minP.TabIndex = 24;
            this.label_info_minP.Text = "Min P";
            // 
            // numericUpDown_minP
            // 
            this.numericUpDown_minP.DecimalPlaces = 2;
            this.numericUpDown_minP.Location = new Point(101, 37);
            this.numericUpDown_minP.Maximum = new decimal(new int[] { 1, 0, 0, 0 });
            this.numericUpDown_minP.Name = "numericUpDown_minP";
            this.numericUpDown_minP.Size = new Size(60, 23);
            this.numericUpDown_minP.TabIndex = 23;
            // 
            // numericUpDown_frequencyPenalty
            // 
            this.numericUpDown_frequencyPenalty.DecimalPlaces = 2;
            this.numericUpDown_frequencyPenalty.Location = new Point(193, 81);
            this.numericUpDown_frequencyPenalty.Maximum = new decimal(new int[] { 4, 0, 0, 0 });
            this.numericUpDown_frequencyPenalty.Name = "numericUpDown_frequencyPenalty";
            this.numericUpDown_frequencyPenalty.Size = new Size(60, 23);
            this.numericUpDown_frequencyPenalty.TabIndex = 27;
            this.numericUpDown_frequencyPenalty.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // label_info_topK
            // 
            this.label_info_topK.AutoSize = true;
            this.label_info_topK.Location = new Point(101, 107);
            this.label_info_topK.Name = "label_info_topK";
            this.label_info_topK.Size = new Size(37, 15);
            this.label_info_topK.TabIndex = 22;
            this.label_info_topK.Text = "Top K";
            // 
            // numericUpDown_topK
            // 
            this.numericUpDown_topK.Location = new Point(101, 125);
            this.numericUpDown_topK.Name = "numericUpDown_topK";
            this.numericUpDown_topK.Size = new Size(60, 23);
            this.numericUpDown_topK.TabIndex = 21;
            this.numericUpDown_topK.Value = new decimal(new int[] { 40, 0, 0, 0 });
            // 
            // label_info_temperature
            // 
            this.label_info_temperature.AutoSize = true;
            this.label_info_temperature.Location = new Point(6, 63);
            this.label_info_temperature.Name = "label_info_temperature";
            this.label_info_temperature.Size = new Size(74, 15);
            this.label_info_temperature.TabIndex = 18;
            this.label_info_temperature.Text = "Temperature";
            // 
            // label_info_repeatPenalty
            // 
            this.label_info_repeatPenalty.AutoSize = true;
            this.label_info_repeatPenalty.Location = new Point(193, 19);
            this.label_info_repeatPenalty.Name = "label_info_repeatPenalty";
            this.label_info_repeatPenalty.Size = new Size(85, 15);
            this.label_info_repeatPenalty.TabIndex = 26;
            this.label_info_repeatPenalty.Text = "Repeat Penalty";
            // 
            // numericUpDown_temperature
            // 
            this.numericUpDown_temperature.DecimalPlaces = 2;
            this.numericUpDown_temperature.Increment = new decimal(new int[] { 1, 0, 0, 131072 });
            this.numericUpDown_temperature.Location = new Point(6, 81);
            this.numericUpDown_temperature.Maximum = new decimal(new int[] { 2, 0, 0, 0 });
            this.numericUpDown_temperature.Name = "numericUpDown_temperature";
            this.numericUpDown_temperature.Size = new Size(60, 23);
            this.numericUpDown_temperature.TabIndex = 17;
            this.numericUpDown_temperature.Value = new decimal(new int[] { 7, 0, 0, 65536 });
            // 
            // numericUpDown_repetitionPenalty
            // 
            this.numericUpDown_repetitionPenalty.DecimalPlaces = 2;
            this.numericUpDown_repetitionPenalty.Location = new Point(193, 37);
            this.numericUpDown_repetitionPenalty.Maximum = new decimal(new int[] { 4, 0, 0, 0 });
            this.numericUpDown_repetitionPenalty.Name = "numericUpDown_repetitionPenalty";
            this.numericUpDown_repetitionPenalty.Size = new Size(60, 23);
            this.numericUpDown_repetitionPenalty.TabIndex = 25;
            this.numericUpDown_repetitionPenalty.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // label_info_topP
            // 
            this.label_info_topP.AutoSize = true;
            this.label_info_topP.Location = new Point(101, 63);
            this.label_info_topP.Name = "label_info_topP";
            this.label_info_topP.Size = new Size(37, 15);
            this.label_info_topP.TabIndex = 20;
            this.label_info_topP.Text = "Top P";
            // 
            // numericUpDown_topP
            // 
            this.numericUpDown_topP.DecimalPlaces = 2;
            this.numericUpDown_topP.Increment = new decimal(new int[] { 1, 0, 0, 131072 });
            this.numericUpDown_topP.Location = new Point(101, 81);
            this.numericUpDown_topP.Maximum = new decimal(new int[] { 1, 0, 0, 0 });
            this.numericUpDown_topP.Name = "numericUpDown_topP";
            this.numericUpDown_topP.Size = new Size(60, 23);
            this.numericUpDown_topP.TabIndex = 19;
            this.numericUpDown_topP.Value = new decimal(new int[] { 9, 0, 0, 65536 });
            // 
            // button_newContext
            // 
            this.button_newContext.Location = new Point(654, 512);
            this.button_newContext.Name = "button_newContext";
            this.button_newContext.Size = new Size(85, 23);
            this.button_newContext.TabIndex = 17;
            this.button_newContext.Text = "New Context";
            this.button_newContext.UseVisualStyleBackColor = true;
            this.button_newContext.Click += this.button_newContext_Click;
            // 
            // button_saveContext
            // 
            this.button_saveContext.Location = new Point(654, 541);
            this.button_saveContext.Name = "button_saveContext";
            this.button_saveContext.Size = new Size(85, 23);
            this.button_saveContext.TabIndex = 18;
            this.button_saveContext.Text = "Save Context";
            this.button_saveContext.UseVisualStyleBackColor = true;
            this.button_saveContext.Click += this.button_saveContext_Click;
            // 
            // richTextBox_conversation
            // 
            this.richTextBox_conversation.Location = new Point(108, 74);
            this.richTextBox_conversation.Name = "richTextBox_conversation";
            this.richTextBox_conversation.Size = new Size(540, 461);
            this.richTextBox_conversation.TabIndex = 19;
            this.richTextBox_conversation.Text = "";
            // 
            // label_tokensGenerated
            // 
            this.label_tokensGenerated.AutoSize = true;
            this.label_tokensGenerated.Location = new Point(12, 501);
            this.label_tokensGenerated.Name = "label_tokensGenerated";
            this.label_tokensGenerated.Size = new Size(55, 15);
            this.label_tokensGenerated.TabIndex = 20;
            this.label_tokensGenerated.Text = "Tokens: -";
            // 
            // label_tokenRate
            // 
            this.label_tokenRate.AutoSize = true;
            this.label_tokenRate.Location = new Point(12, 486);
            this.label_tokenRate.Name = "label_tokenRate";
            this.label_tokenRate.Size = new Size(47, 15);
            this.label_tokenRate.TabIndex = 21;
            this.label_tokenRate.Text = "Tok/s: -";
            // 
            // button_loadContext
            // 
            this.button_loadContext.Location = new Point(654, 570);
            this.button_loadContext.Name = "button_loadContext";
            this.button_loadContext.Size = new Size(85, 23);
            this.button_loadContext.TabIndex = 22;
            this.button_loadContext.Text = "Load Context";
            this.button_loadContext.UseVisualStyleBackColor = true;
            this.button_loadContext.Click += this.button_loadContext_Click;
            // 
            // label_currentlySavedContextPath
            // 
            this.label_currentlySavedContextPath.AutoSize = true;
            this.label_currentlySavedContextPath.Location = new Point(108, 56);
            this.label_currentlySavedContextPath.Name = "label_currentlySavedContextPath";
            this.label_currentlySavedContextPath.Size = new Size(124, 15);
            this.label_currentlySavedContextPath.TabIndex = 23;
            this.label_currentlySavedContextPath.Text = "- Temporary Context -";
            // 
            // checkBox_autoSave
            // 
            this.checkBox_autoSave.AutoSize = true;
            this.checkBox_autoSave.Checked = true;
            this.checkBox_autoSave.CheckState = CheckState.Checked;
            this.checkBox_autoSave.Location = new Point(567, 544);
            this.checkBox_autoSave.Name = "checkBox_autoSave";
            this.checkBox_autoSave.RightToLeft = RightToLeft.Yes;
            this.checkBox_autoSave.Size = new Size(81, 19);
            this.checkBox_autoSave.TabIndex = 31;
            this.checkBox_autoSave.Text = "Auto-Save";
            this.checkBox_autoSave.UseVisualStyleBackColor = true;
            // 
            // label_tokenQuota
            // 
            this.label_tokenQuota.AutoSize = true;
            this.label_tokenQuota.Location = new Point(110, 41);
            this.label_tokenQuota.Name = "label_tokenQuota";
            this.label_tokenQuota.Size = new Size(77, 15);
            this.label_tokenQuota.TabIndex = 32;
            this.label_tokenQuota.Text = "(0 / - Tokens)";
            // 
            // WindowMain
            // 
            this.AutoScaleDimensions = new SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(1424, 681);
            this.Controls.Add(this.label_tokenQuota);
            this.Controls.Add(this.checkBox_autoSave);
            this.Controls.Add(this.label_currentlySavedContextPath);
            this.Controls.Add(this.button_loadContext);
            this.Controls.Add(this.label_tokenRate);
            this.Controls.Add(this.label_tokensGenerated);
            this.Controls.Add(this.richTextBox_conversation);
            this.Controls.Add(this.button_saveContext);
            this.Controls.Add(this.button_newContext);
            this.Controls.Add(this.groupBox_inferenceParams);
            this.Controls.Add(this.label_attachedImages);
            this.Controls.Add(this.button_images);
            this.Controls.Add(this.label_elapsed);
            this.Controls.Add(this.button_send);
            this.Controls.Add(this.textBox_prompt);
            this.Controls.Add(this.label_info_contextSize);
            this.Controls.Add(this.numericUpDown_contextSize);
            this.Controls.Add(this.checkBox_warmupMmproj);
            this.Controls.Add(this.checkBox_tryLoadMmproj);
            this.Controls.Add(this.label_info_gpuLayerCount);
            this.Controls.Add(this.numericUpDown_gpuLayerCount);
            this.Controls.Add(this.button_initialize);
            this.Controls.Add(this.comboBox_models);
            this.Controls.Add(this.listBox_log);
            this.MaximumSize = new Size(1440, 720);
            this.MinimumSize = new Size(1440, 720);
            this.Name = "WindowMain";
            this.Text = "FastLlamaSharp (Forms)";
            ((System.ComponentModel.ISupportInitialize) this.numericUpDown_gpuLayerCount).EndInit();
            ((System.ComponentModel.ISupportInitialize) this.numericUpDown_contextSize).EndInit();
            this.groupBox_inferenceParams.ResumeLayout(false);
            this.groupBox_inferenceParams.PerformLayout();
            ((System.ComponentModel.ISupportInitialize) this.numericUpDown_maxTokens).EndInit();
            ((System.ComponentModel.ISupportInitialize) this.numericUpDown_minP).EndInit();
            ((System.ComponentModel.ISupportInitialize) this.numericUpDown_frequencyPenalty).EndInit();
            ((System.ComponentModel.ISupportInitialize) this.numericUpDown_topK).EndInit();
            ((System.ComponentModel.ISupportInitialize) this.numericUpDown_temperature).EndInit();
            ((System.ComponentModel.ISupportInitialize) this.numericUpDown_repetitionPenalty).EndInit();
            ((System.ComponentModel.ISupportInitialize) this.numericUpDown_topP).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private ListBox listBox_log;
        private ComboBox comboBox_models;
        private Button button_initialize;
        private NumericUpDown numericUpDown_gpuLayerCount;
        private Label label_info_gpuLayerCount;
        private CheckBox checkBox_tryLoadMmproj;
        private CheckBox checkBox_warmupMmproj;
        private Label label_info_contextSize;
        private NumericUpDown numericUpDown_contextSize;
        private TextBox textBox_prompt;
        private Button button_send;
        private Label label_elapsed;
        private Button button_images;
        private Label label_attachedImages;
        private CheckBox checkBox_isolated;
        private GroupBox groupBox_inferenceParams;
        private Label label_info_topK;
        private NumericUpDown numericUpDown_topK;
        private Label label_info_topP;
        private NumericUpDown numericUpDown_topP;
        private Label label_info_temperature;
        private NumericUpDown numericUpDown_temperature;
        private Label label_info_frequencyPenatlty;
        private NumericUpDown numericUpDown_frequencyPenalty;
        private Label label_info_repeatPenalty;
        private NumericUpDown numericUpDown_repetitionPenalty;
        private Label label_info_minP;
        private NumericUpDown numericUpDown_minP;
        private Label label_info_maxTokens;
        private NumericUpDown numericUpDown_maxTokens;
        private Button button_resetParams;
        private Button button_newContext;
        private Button button_saveContext;
        private RichTextBox richTextBox_conversation;
        private Label label_tokensGenerated;
        private Label label_tokenRate;
        private Button button_loadContext;
        private Label label_currentlySavedContextPath;
        private CheckBox checkBox_autoSave;
        private Label label_tokenQuota;
    }
}
