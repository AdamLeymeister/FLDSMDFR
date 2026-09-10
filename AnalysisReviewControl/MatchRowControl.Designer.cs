namespace AnalysisReviewControl
{
    partial class MatchRowControl
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.lblFound = new System.Windows.Forms.Label();
            this.chkAccurate = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // lblFound
            // 
            this.lblFound.AutoSize = true;
            this.lblFound.Location = new System.Drawing.Point(36, 104);
            this.lblFound.Name = "lblFound";
            this.lblFound.Size = new System.Drawing.Size(82, 25);
            this.lblFound.TabIndex = 0;
            this.lblFound.Text = "lblFound";
            // 
            // chkAccurate
            // 
            this.chkAccurate.AutoSize = true;
            this.chkAccurate.Location = new System.Drawing.Point(331, 119);
            this.chkAccurate.Name = "chkAccurate";
            this.chkAccurate.Size = new System.Drawing.Size(121, 29);
            this.chkAccurate.TabIndex = 1;
            this.chkAccurate.Text = "checkBox1";
            this.chkAccurate.UseVisualStyleBackColor = true;
            // 
            // MatchRowControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.chkAccurate);
            this.Controls.Add(this.lblFound);
            this.Name = "MatchRowControl";
            this.Size = new System.Drawing.Size(497, 352);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Label lblFound;
        private CheckBox chkAccurate;
    }
}
