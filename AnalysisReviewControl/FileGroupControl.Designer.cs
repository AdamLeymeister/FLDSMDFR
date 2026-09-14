namespace AnalysisReviewControl
{
    partial class FileGroupControl
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
            this.pnlHeader = new System.Windows.Forms.Panel();
            this.chkBulk = new System.Windows.Forms.CheckBox();
            this.lblCount = new System.Windows.Forms.Label();
            this.lblFile = new System.Windows.Forms.Label();
            this.btnExpand = new System.Windows.Forms.Button();
            this.pnlMatches = new System.Windows.Forms.Panel();
            this.pnlHeader.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnlHeader
            // 
            this.pnlHeader.Controls.Add(this.chkBulk);
            this.pnlHeader.Controls.Add(this.lblCount);
            this.pnlHeader.Controls.Add(this.lblFile);
            this.pnlHeader.Controls.Add(this.btnExpand);
            this.pnlHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlHeader.Location = new System.Drawing.Point(0, 0);
            this.pnlHeader.Name = "pnlHeader";
            this.pnlHeader.Size = new System.Drawing.Size(876, 150);
            this.pnlHeader.TabIndex = 0;
            // 
            // chkBulk
            // 
            this.chkBulk.AutoSize = true;
            this.chkBulk.Location = new System.Drawing.Point(413, 58);
            this.chkBulk.Name = "chkBulk";
            this.chkBulk.Size = new System.Drawing.Size(121, 29);
            this.chkBulk.TabIndex = 3;
            this.chkBulk.Text = "checkBox1";
            this.chkBulk.UseVisualStyleBackColor = true;
            // 
            // lblCount
            // 
            this.lblCount.AutoSize = true;
            this.lblCount.Location = new System.Drawing.Point(258, 63);
            this.lblCount.Name = "lblCount";
            this.lblCount.Size = new System.Drawing.Size(59, 25);
            this.lblCount.TabIndex = 2;
            this.lblCount.Text = "label1";
            // 
            // lblFile
            // 
            this.lblFile.AutoSize = true;
            this.lblFile.Location = new System.Drawing.Point(166, 60);
            this.lblFile.Name = "lblFile";
            this.lblFile.Size = new System.Drawing.Size(59, 25);
            this.lblFile.TabIndex = 1;
            this.lblFile.Text = "label1";
            // 
            // btnExpand
            // 
            this.btnExpand.Location = new System.Drawing.Point(24, 53);
            this.btnExpand.Name = "btnExpand";
            this.btnExpand.Size = new System.Drawing.Size(112, 34);
            this.btnExpand.TabIndex = 0;
            this.btnExpand.Text = "button1";
            this.btnExpand.UseVisualStyleBackColor = true;
            // 
            // pnlMatches
            // 
            this.pnlMatches.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlMatches.Location = new System.Drawing.Point(0, 150);
            this.pnlMatches.Name = "pnlMatches";
            this.pnlMatches.Size = new System.Drawing.Size(876, 150);
            this.pnlMatches.TabIndex = 1;
            // 
            // FileGroupControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.pnlMatches);
            this.Controls.Add(this.pnlHeader);
            this.Name = "FileGroupControl";
            this.Size = new System.Drawing.Size(876, 484);
            this.pnlHeader.ResumeLayout(false);
            this.pnlHeader.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private Panel pnlHeader;
        private CheckBox chkBulk;
        private Label lblCount;
        private Label lblFile;
        private Button btnExpand;
        private Panel pnlMatches;
    }
}
