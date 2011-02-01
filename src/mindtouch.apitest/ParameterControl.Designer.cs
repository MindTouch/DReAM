namespace MindTouch.Tools {
    partial class ParameterControl {
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.paramLabel = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.includeCheckBox = new System.Windows.Forms.CheckBox();
            this.paramTextBox = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // paramLabel
            // 
            this.paramLabel.AutoSize = true;
            this.paramLabel.Dock = System.Windows.Forms.DockStyle.Left;
            this.paramLabel.Location = new System.Drawing.Point(0, 0);
            this.paramLabel.Name = "paramLabel";
            this.paramLabel.Size = new System.Drawing.Size(35, 13);
            this.paramLabel.TabIndex = 0;
            this.paramLabel.Text = "label1";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Dock = System.Windows.Forms.DockStyle.Left;
            this.label1.Location = new System.Drawing.Point(35, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(22, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = ":    ";
            // 
            // includeCheckBox
            // 
            this.includeCheckBox.AutoSize = true;
            this.includeCheckBox.Location = new System.Drawing.Point(649, 2);
            this.includeCheckBox.Name = "includeCheckBox";
            this.includeCheckBox.Size = new System.Drawing.Size(86, 17);
            this.includeCheckBox.TabIndex = 3;
            this.includeCheckBox.Text = "Include in url";
            this.includeCheckBox.UseVisualStyleBackColor = true;
            this.includeCheckBox.CheckedChanged += new System.EventHandler(this.includeCheckBox_CheckedChanged);
            // 
            // paramTextBox
            // 
            this.paramTextBox.Location = new System.Drawing.Point(108, 0);
            this.paramTextBox.Name = "paramTextBox";
            this.paramTextBox.Size = new System.Drawing.Size(525, 20);
            this.paramTextBox.TabIndex = 4;
            this.paramTextBox.TextChanged += new System.EventHandler(this.paramTextBox_TextChanged);
            // 
            // ParameterControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.paramTextBox);
            this.Controls.Add(this.includeCheckBox);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.paramLabel);
            this.Name = "ParameterControl";
            this.Size = new System.Drawing.Size(793, 23);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label paramLabel;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.CheckBox includeCheckBox;
        private System.Windows.Forms.TextBox paramTextBox;
    }
}
