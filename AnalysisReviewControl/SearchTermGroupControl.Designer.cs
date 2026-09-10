namespace AnalysisReviewControl
{
    partial class SearchTermGroupControl
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
            this.btnExpand = new System.Windows.Forms.Button();
            this.lblSearchTerm = new System.Windows.Forms.Label();
            this.lblCount = new System.Windows.Forms.Label();
            this.chkBulk = new System.Windows.Forms.CheckBox();
            this.pnlFiles = new System.Windows.Forms.Panel();
            this.pnlHeader.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnlHeader
            // 
            this.pnlHeader.Controls.Add(this.chkBulk);
            this.pnlHeader.Controls.Add(this.lblCount);
            this.pnlHeader.Controls.Add(this.lblSearchTerm);
            this.pnlHeader.Controls.Add(this.btnExpand);
            this.pnlHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlHeader.Location = new System.Drawing.Point(0, 0);
            this.pnlHeader.Name = "pnlHeader";
            this.pnlHeader.Size = new System.Drawing.Size(473, 150);
            this.pnlHeader.TabIndex = 0;
            // 
            // btnExpand
            // 
            this.btnExpand.Location = new System.Drawing.Point(26, 48);
            this.btnExpand.Name = "btnExpand";
            this.btnExpand.Size = new System.Drawing.Size(112, 34);
            this.btnExpand.TabIndex = 0;
            this.btnExpand.Text = "button1";
            this.btnExpand.UseVisualStyleBackColor = true;
            // 
            // lblSearchTerm
            // 
            this.lblSearchTerm.AutoSize = true;
            this.lblSearchTerm.Location = new System.Drawing.Point(144, 54);
            this.lblSearchTerm.Name = "lblSearchTerm";
            this.lblSearchTerm.Size = new System.Drawing.Size(59, 25);
            this.lblSearchTerm.TabIndex = 1;
            this.lblSearchTerm.Text = "label1";
            // 
            // lblCount
            // 
            this.lblCount.AutoSize = true;
            this.lblCount.Location = new System.Drawing.Point(229, 56);
            this.lblCount.Name = "lblCount";
            this.lblCount.Size = new System.Drawing.Size(59, 25);
            this.lblCount.TabIndex = 2;
            this.lblCount.Text = "label1";
            // 
            // chkBulk
            // 
            this.chkBulk.AutoSize = true;
            this.chkBulk.Location = new System.Drawing.Point(301, 61);
            this.chkBulk.Name = "chkBulk";
            this.chkBulk.Size = new System.Drawing.Size(121, 29);
            this.chkBulk.TabIndex = 3;
            this.chkBulk.Text = "checkBox1";
            this.chkBulk.UseVisualStyleBackColor = true;
            // 
            // pnlFiles
            // 
            this.pnlFiles.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlFiles.Location = new System.Drawing.Point(0, 150);
            this.pnlFiles.Name = "pnlFiles";
            this.pnlFiles.Size = new System.Drawing.Size(473, 150);
            this.pnlFiles.TabIndex = 1;
            // 
            // SearchTermGroupControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.pnlFiles);
            this.Controls.Add(this.pnlHeader);
            this.Name = "SearchTermGroupControl";
            this.Size = new System.Drawing.Size(473, 296);
            this.pnlHeader.ResumeLayout(false);
            this.pnlHeader.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private Panel pnlHeader;
        private CheckBox chkBulk;
        private Label lblCount;
        private Label lblSearchTerm;
        private Button btnExpand;
        private Panel pnlFiles;
    }
}
