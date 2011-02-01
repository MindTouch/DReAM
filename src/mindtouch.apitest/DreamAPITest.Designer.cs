namespace MindTouch.Tools {
    partial class DreamAPITest {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.apiComboBox = new System.Windows.Forms.ComboBox();
            this.apiLabel = new System.Windows.Forms.Label();
            this.submitButton = new System.Windows.Forms.Button();
            this.loginLabel = new System.Windows.Forms.Label();
            this.loginTextBox = new System.Windows.Forms.TextBox();
            this.passwordLabel = new System.Windows.Forms.Label();
            this.passwordTextBox = new System.Windows.Forms.TextBox();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.loadBlueprintFromToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.invokeAPITabPage = new System.Windows.Forms.TabPage();
            this.messageContentLabel = new System.Windows.Forms.Label();
            this.messageContentPanel = new System.Windows.Forms.Panel();
            this.openFileButton = new System.Windows.Forms.Button();
            this.filepathTextBox = new System.Windows.Forms.TextBox();
            this.messageContentTextBox = new System.Windows.Forms.RichTextBox();
            this.useTextRadioButton = new System.Windows.Forms.RadioButton();
            this.useFileRadioButton = new System.Windows.Forms.RadioButton();
            this.submitPanel = new System.Windows.Forms.Panel();
            this.updateUrlButton = new System.Windows.Forms.Button();
            this.urlTextBox = new System.Windows.Forms.TextBox();
            this.parametersLabel = new System.Windows.Forms.Label();
            this.connectionPanel = new System.Windows.Forms.Panel();
            this.parametersPanel = new System.Windows.Forms.Panel();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.resultsTabPage = new System.Windows.Forms.TabPage();
            this.txtHeaders = new System.Windows.Forms.TextBox();
            this.webBrowser = new System.Windows.Forms.WebBrowser();
            this.pageSetupDialog1 = new System.Windows.Forms.PageSetupDialog();
            this.mimeTypeLabel = new System.Windows.Forms.Label();
            this.mimeTypeComboBox = new System.Windows.Forms.ComboBox();
            this.menuStrip1.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.invokeAPITabPage.SuspendLayout();
            this.messageContentPanel.SuspendLayout();
            this.submitPanel.SuspendLayout();
            this.connectionPanel.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.resultsTabPage.SuspendLayout();
            this.SuspendLayout();
            // 
            // apiComboBox
            // 
            this.apiComboBox.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.apiComboBox.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.apiComboBox.FormattingEnabled = true;
            this.apiComboBox.Location = new System.Drawing.Point(102, 70);
            this.apiComboBox.Name = "apiComboBox";
            this.apiComboBox.Size = new System.Drawing.Size(531, 21);
            this.apiComboBox.TabIndex = 0;
            this.apiComboBox.SelectedIndexChanged += new System.EventHandler(this.apiComboBox_SelectedIndexChanged);
            this.apiComboBox.TextUpdate += new System.EventHandler(this.apiComboBox_TextUpdate);
            // 
            // apiLabel
            // 
            this.apiLabel.AutoSize = true;
            this.apiLabel.Location = new System.Drawing.Point(4, 70);
            this.apiLabel.Name = "apiLabel";
            this.apiLabel.Size = new System.Drawing.Size(27, 13);
            this.apiLabel.TabIndex = 3;
            this.apiLabel.Text = "API:";
            // 
            // submitButton
            // 
            this.submitButton.Location = new System.Drawing.Point(662, 42);
            this.submitButton.Name = "submitButton";
            this.submitButton.Size = new System.Drawing.Size(110, 27);
            this.submitButton.TabIndex = 4;
            this.submitButton.Text = "Submit";
            this.submitButton.UseVisualStyleBackColor = true;
            this.submitButton.Click += new System.EventHandler(this.submitButton_Click);
            // 
            // loginLabel
            // 
            this.loginLabel.AutoSize = true;
            this.loginLabel.Location = new System.Drawing.Point(4, 20);
            this.loginLabel.Name = "loginLabel";
            this.loginLabel.Size = new System.Drawing.Size(36, 13);
            this.loginLabel.TabIndex = 6;
            this.loginLabel.Text = "Login:";
            // 
            // loginTextBox
            // 
            this.loginTextBox.Location = new System.Drawing.Point(102, 20);
            this.loginTextBox.Name = "loginTextBox";
            this.loginTextBox.Size = new System.Drawing.Size(531, 20);
            this.loginTextBox.TabIndex = 5;
            this.loginTextBox.Text = "Sysop";
            // 
            // passwordLabel
            // 
            this.passwordLabel.AutoSize = true;
            this.passwordLabel.Location = new System.Drawing.Point(3, 45);
            this.passwordLabel.Name = "passwordLabel";
            this.passwordLabel.Size = new System.Drawing.Size(56, 13);
            this.passwordLabel.TabIndex = 8;
            this.passwordLabel.Text = "Password:";
            // 
            // passwordTextBox
            // 
            this.passwordTextBox.Location = new System.Drawing.Point(102, 45);
            this.passwordTextBox.Name = "passwordTextBox";
            this.passwordTextBox.Size = new System.Drawing.Size(531, 20);
            this.passwordTextBox.TabIndex = 7;
            this.passwordTextBox.UseSystemPasswordChar = true;
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(909, 24);
            this.menuStrip1.TabIndex = 18;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.loadBlueprintFromToolStripMenuItem,
            this.toolStripSeparator1,
            this.exitToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(35, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // loadBlueprintFromToolStripMenuItem
            // 
            this.loadBlueprintFromToolStripMenuItem.Name = "loadBlueprintFromToolStripMenuItem";
            this.loadBlueprintFromToolStripMenuItem.Size = new System.Drawing.Size(181, 22);
            this.loadBlueprintFromToolStripMenuItem.Text = "Load Blueprint From...";
            this.loadBlueprintFromToolStripMenuItem.Click += new System.EventHandler(this.loadBlueprintFromToolStripMenuItem_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(178, 6);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(181, 22);
            this.exitToolStripMenuItem.Text = "Exit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.invokeAPITabPage);
            this.tabControl1.Controls.Add(this.resultsTabPage);
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl1.Location = new System.Drawing.Point(0, 24);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(909, 642);
            this.tabControl1.TabIndex = 20;
            // 
            // invokeAPITabPage
            // 
            this.invokeAPITabPage.BackColor = System.Drawing.Color.Transparent;
            this.invokeAPITabPage.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.invokeAPITabPage.Controls.Add(this.messageContentLabel);
            this.invokeAPITabPage.Controls.Add(this.messageContentPanel);
            this.invokeAPITabPage.Controls.Add(this.submitPanel);
            this.invokeAPITabPage.Controls.Add(this.parametersLabel);
            this.invokeAPITabPage.Controls.Add(this.connectionPanel);
            this.invokeAPITabPage.Controls.Add(this.parametersPanel);
            this.invokeAPITabPage.Controls.Add(this.statusStrip1);
            this.invokeAPITabPage.Location = new System.Drawing.Point(4, 22);
            this.invokeAPITabPage.Name = "invokeAPITabPage";
            this.invokeAPITabPage.Padding = new System.Windows.Forms.Padding(3);
            this.invokeAPITabPage.Size = new System.Drawing.Size(901, 616);
            this.invokeAPITabPage.TabIndex = 0;
            this.invokeAPITabPage.Text = "Invoke API";
            // 
            // messageContentLabel
            // 
            this.messageContentLabel.AutoSize = true;
            this.messageContentLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Underline, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.messageContentLabel.Location = new System.Drawing.Point(53, 301);
            this.messageContentLabel.Name = "messageContentLabel";
            this.messageContentLabel.Size = new System.Drawing.Size(93, 13);
            this.messageContentLabel.TabIndex = 18;
            this.messageContentLabel.Text = "Message Content:";
            this.messageContentLabel.Visible = false;
            // 
            // messageContentPanel
            // 
            this.messageContentPanel.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.messageContentPanel.Controls.Add(this.mimeTypeLabel);
            this.messageContentPanel.Controls.Add(this.mimeTypeComboBox);
            this.messageContentPanel.Controls.Add(this.openFileButton);
            this.messageContentPanel.Controls.Add(this.filepathTextBox);
            this.messageContentPanel.Controls.Add(this.messageContentTextBox);
            this.messageContentPanel.Controls.Add(this.useTextRadioButton);
            this.messageContentPanel.Controls.Add(this.useFileRadioButton);
            this.messageContentPanel.Location = new System.Drawing.Point(56, 319);
            this.messageContentPanel.Name = "messageContentPanel";
            this.messageContentPanel.Size = new System.Drawing.Size(776, 175);
            this.messageContentPanel.TabIndex = 17;
            this.messageContentPanel.Visible = false;
            // 
            // openFileButton
            // 
            this.openFileButton.Location = new System.Drawing.Point(639, 13);
            this.openFileButton.Name = "openFileButton";
            this.openFileButton.Size = new System.Drawing.Size(35, 21);
            this.openFileButton.TabIndex = 20;
            this.openFileButton.Text = "...";
            this.openFileButton.UseVisualStyleBackColor = true;
            this.openFileButton.Click += new System.EventHandler(this.openFileButton_Click);
            // 
            // filepathTextBox
            // 
            this.filepathTextBox.Location = new System.Drawing.Point(102, 13);
            this.filepathTextBox.Name = "filepathTextBox";
            this.filepathTextBox.Size = new System.Drawing.Size(531, 20);
            this.filepathTextBox.TabIndex = 19;
            this.filepathTextBox.TextChanged += new System.EventHandler(this.filepathTextBox_TextChanged);
            // 
            // messageContentTextBox
            // 
            this.messageContentTextBox.Location = new System.Drawing.Point(102, 35);
            this.messageContentTextBox.Name = "messageContentTextBox";
            this.messageContentTextBox.Size = new System.Drawing.Size(531, 111);
            this.messageContentTextBox.TabIndex = 18;
            this.messageContentTextBox.Text = "";
            // 
            // useTextRadioButton
            // 
            this.useTextRadioButton.AutoSize = true;
            this.useTextRadioButton.Checked = true;
            this.useTextRadioButton.Location = new System.Drawing.Point(16, 35);
            this.useTextRadioButton.Name = "useTextRadioButton";
            this.useTextRadioButton.Size = new System.Drawing.Size(71, 17);
            this.useTextRadioButton.TabIndex = 17;
            this.useTextRadioButton.TabStop = true;
            this.useTextRadioButton.Text = "Use Text:";
            this.useTextRadioButton.UseVisualStyleBackColor = true;
            // 
            // useFileRadioButton
            // 
            this.useFileRadioButton.AutoSize = true;
            this.useFileRadioButton.Location = new System.Drawing.Point(16, 13);
            this.useFileRadioButton.Name = "useFileRadioButton";
            this.useFileRadioButton.Size = new System.Drawing.Size(66, 17);
            this.useFileRadioButton.TabIndex = 16;
            this.useFileRadioButton.Text = "Use File:";
            this.useFileRadioButton.UseVisualStyleBackColor = true;
            // 
            // submitPanel
            // 
            this.submitPanel.Controls.Add(this.updateUrlButton);
            this.submitPanel.Controls.Add(this.urlTextBox);
            this.submitPanel.Controls.Add(this.submitButton);
            this.submitPanel.Location = new System.Drawing.Point(56, 501);
            this.submitPanel.Name = "submitPanel";
            this.submitPanel.Size = new System.Drawing.Size(775, 75);
            this.submitPanel.TabIndex = 15;
            this.submitPanel.Visible = false;
            // 
            // updateUrlButton
            // 
            this.updateUrlButton.Location = new System.Drawing.Point(662, 9);
            this.updateUrlButton.Name = "updateUrlButton";
            this.updateUrlButton.Size = new System.Drawing.Size(110, 27);
            this.updateUrlButton.TabIndex = 14;
            this.updateUrlButton.Text = "Update URL";
            this.updateUrlButton.UseVisualStyleBackColor = true;
            this.updateUrlButton.Click += new System.EventHandler(this.updateUrlButton_Click);
            // 
            // urlTextBox
            // 
            this.urlTextBox.Location = new System.Drawing.Point(0, 13);
            this.urlTextBox.Name = "urlTextBox";
            this.urlTextBox.Size = new System.Drawing.Size(649, 20);
            this.urlTextBox.TabIndex = 9;
            // 
            // parametersLabel
            // 
            this.parametersLabel.AutoSize = true;
            this.parametersLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Underline, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.parametersLabel.Location = new System.Drawing.Point(53, 137);
            this.parametersLabel.Name = "parametersLabel";
            this.parametersLabel.Size = new System.Drawing.Size(63, 13);
            this.parametersLabel.TabIndex = 13;
            this.parametersLabel.Text = "Parameters:";
            this.parametersLabel.Visible = false;
            // 
            // connectionPanel
            // 
            this.connectionPanel.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.connectionPanel.Controls.Add(this.loginLabel);
            this.connectionPanel.Controls.Add(this.passwordTextBox);
            this.connectionPanel.Controls.Add(this.passwordLabel);
            this.connectionPanel.Controls.Add(this.loginTextBox);
            this.connectionPanel.Controls.Add(this.apiLabel);
            this.connectionPanel.Controls.Add(this.apiComboBox);
            this.connectionPanel.Location = new System.Drawing.Point(56, 21);
            this.connectionPanel.Name = "connectionPanel";
            this.connectionPanel.Size = new System.Drawing.Size(776, 100);
            this.connectionPanel.TabIndex = 12;
            this.connectionPanel.Visible = false;
            // 
            // parametersPanel
            // 
            this.parametersPanel.AutoScroll = true;
            this.parametersPanel.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.parametersPanel.Location = new System.Drawing.Point(56, 153);
            this.parametersPanel.Name = "parametersPanel";
            this.parametersPanel.Size = new System.Drawing.Size(776, 132);
            this.parametersPanel.TabIndex = 10;
            this.parametersPanel.Visible = false;
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel});
            this.statusStrip1.Location = new System.Drawing.Point(3, 587);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(891, 22);
            this.statusStrip1.TabIndex = 9;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // toolStripStatusLabel
            // 
            this.toolStripStatusLabel.Name = "toolStripStatusLabel";
            this.toolStripStatusLabel.Size = new System.Drawing.Size(100, 17);
            this.toolStripStatusLabel.Text = "No blueprint loaded";
            // 
            // resultsTabPage
            // 
            this.resultsTabPage.BackColor = System.Drawing.Color.Transparent;
            this.resultsTabPage.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.resultsTabPage.Controls.Add(this.txtHeaders);
            this.resultsTabPage.Controls.Add(this.webBrowser);
            this.resultsTabPage.Location = new System.Drawing.Point(4, 22);
            this.resultsTabPage.Name = "resultsTabPage";
            this.resultsTabPage.Padding = new System.Windows.Forms.Padding(3);
            this.resultsTabPage.Size = new System.Drawing.Size(901, 616);
            this.resultsTabPage.TabIndex = 1;
            this.resultsTabPage.Text = "Results";
            // 
            // txtHeaders
            // 
            this.txtHeaders.Dock = System.Windows.Forms.DockStyle.Top;
            this.txtHeaders.Location = new System.Drawing.Point(3, 3);
            this.txtHeaders.Multiline = true;
            this.txtHeaders.Name = "txtHeaders";
            this.txtHeaders.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtHeaders.Size = new System.Drawing.Size(891, 99);
            this.txtHeaders.TabIndex = 1;
            // 
            // webBrowser
            // 
            this.webBrowser.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.webBrowser.Location = new System.Drawing.Point(3, 108);
            this.webBrowser.MinimumSize = new System.Drawing.Size(20, 20);
            this.webBrowser.Name = "webBrowser";
            this.webBrowser.Size = new System.Drawing.Size(891, 501);
            this.webBrowser.TabIndex = 0;
            this.webBrowser.Navigating += new System.Windows.Forms.WebBrowserNavigatingEventHandler(this.webBrowser_Navigating);
            // 
            // mimeTypeLabel
            // 
            this.mimeTypeLabel.AutoSize = true;
            this.mimeTypeLabel.Location = new System.Drawing.Point(13, 147);
            this.mimeTypeLabel.Name = "mimeTypeLabel";
            this.mimeTypeLabel.Size = new System.Drawing.Size(61, 13);
            this.mimeTypeLabel.TabIndex = 22;
            this.mimeTypeLabel.Text = "MIME type:";
            // 
            // mimeTypeComboBox
            // 
            this.mimeTypeComboBox.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.mimeTypeComboBox.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.mimeTypeComboBox.FormattingEnabled = true;
            this.mimeTypeComboBox.Items.AddRange(new object[] {
            "text/plain",
            "text/xml",
            "image/bmp",
            "image/gif",
            "image/jpeg",
            "image/png",
            "image/tiff"});
            this.mimeTypeComboBox.Location = new System.Drawing.Point(102, 147);
            this.mimeTypeComboBox.Name = "mimeTypeComboBox";
            this.mimeTypeComboBox.Size = new System.Drawing.Size(531, 21);
            this.mimeTypeComboBox.TabIndex = 21;
            this.mimeTypeComboBox.Text = "text/plain";
            // 
            // DreamAPITest
            // 
            this.AcceptButton = this.submitButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(909, 666);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.menuStrip1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.Name = "DreamAPITest";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "MindTouch API Test";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.APIManualTest_FormClosed);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.tabControl1.ResumeLayout(false);
            this.invokeAPITabPage.ResumeLayout(false);
            this.invokeAPITabPage.PerformLayout();
            this.messageContentPanel.ResumeLayout(false);
            this.messageContentPanel.PerformLayout();
            this.submitPanel.ResumeLayout(false);
            this.submitPanel.PerformLayout();
            this.connectionPanel.ResumeLayout(false);
            this.connectionPanel.PerformLayout();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.resultsTabPage.ResumeLayout(false);
            this.resultsTabPage.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox apiComboBox;
        private System.Windows.Forms.Label apiLabel;
        private System.Windows.Forms.Button submitButton;
        private System.Windows.Forms.Label loginLabel;
        private System.Windows.Forms.TextBox loginTextBox;
        private System.Windows.Forms.Label passwordLabel;
        private System.Windows.Forms.TextBox passwordTextBox;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem loadBlueprintFromToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage invokeAPITabPage;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.TabPage resultsTabPage;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel;
        private System.Windows.Forms.Panel parametersPanel;
        private System.Windows.Forms.Panel connectionPanel;
        private System.Windows.Forms.Label parametersLabel;
        private System.Windows.Forms.PageSetupDialog pageSetupDialog1;
        private System.Windows.Forms.Button updateUrlButton;
        private System.Windows.Forms.TextBox urlTextBox;
        private System.Windows.Forms.Panel submitPanel;
        private System.Windows.Forms.WebBrowser webBrowser;
        private System.Windows.Forms.Panel messageContentPanel;
        private System.Windows.Forms.RadioButton useTextRadioButton;
        private System.Windows.Forms.RadioButton useFileRadioButton;
        private System.Windows.Forms.RichTextBox messageContentTextBox;
        private System.Windows.Forms.Label messageContentLabel;
        private System.Windows.Forms.Button openFileButton;
        private System.Windows.Forms.TextBox filepathTextBox;
        private System.Windows.Forms.TextBox txtHeaders;
        private System.Windows.Forms.Label mimeTypeLabel;
        private System.Windows.Forms.ComboBox mimeTypeComboBox;


    }
}

