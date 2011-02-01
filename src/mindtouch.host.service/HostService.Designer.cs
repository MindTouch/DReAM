namespace MindTouch.Dream {
    partial class WindowsServiceHost {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if(disposing && (components != null)) {
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
            this._sysEventLog = new System.Diagnostics.EventLog();
            ((System.ComponentModel.ISupportInitialize)(this._sysEventLog)).BeginInit();
            // 
            // WindowsServiceHost
            // 
            this.ServiceName = "MindTouch Dream";
            ((System.ComponentModel.ISupportInitialize)(this._sysEventLog)).EndInit();

        }

        #endregion

        private System.Diagnostics.EventLog _sysEventLog;
    }
}
