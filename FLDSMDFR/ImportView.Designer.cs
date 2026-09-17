namespace FLDSMDFR
{
    partial class ImportView
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
            this.pnlContent = new System.Windows.Forms.Panel();
            this.tlpImport = new System.Windows.Forms.TableLayoutPanel();
            this.pnlCardActivity = new System.Windows.Forms.Panel();
            this.pnlReview = new System.Windows.Forms.Panel();
            this.pnlContent.SuspendLayout();
            this.tlpImport.SuspendLayout();
            this.pnlCardActivity.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnlContent
            // 
            this.pnlContent.Controls.Add(this.tlpImport);
            this.pnlContent.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlContent.Location = new System.Drawing.Point(0, 0);
            this.pnlContent.Name = "pnlContent";
            this.pnlContent.Padding = new System.Windows.Forms.Padding(20);
            this.pnlContent.Size = new System.Drawing.Size(1359, 783);
            this.pnlContent.TabIndex = 0;
            // 
            // tlpImport
            // 
            this.tlpImport.ColumnCount = 1;
            this.tlpImport.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpImport.Controls.Add(this.pnlCardActivity, 0, 0);
            this.tlpImport.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpImport.Location = new System.Drawing.Point(20, 20);
            this.tlpImport.Name = "tlpImport";
            this.tlpImport.RowCount = 1;
            this.tlpImport.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpImport.Size = new System.Drawing.Size(1319, 743);
            this.tlpImport.TabIndex = 0;
            // 
            // pnlCardActivity
            // 
            this.pnlCardActivity.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(32)))), ((int)(((byte)(44)))));
            this.pnlCardActivity.Controls.Add(this.pnlReview);
            this.pnlCardActivity.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlCardActivity.Location = new System.Drawing.Point(8, 8);
            this.pnlCardActivity.Margin = new System.Windows.Forms.Padding(8);
            this.pnlCardActivity.Name = "pnlCardActivity";
            this.pnlCardActivity.Size = new System.Drawing.Size(1303, 727);
            this.pnlCardActivity.TabIndex = 0;
            // 
            // pnlReview
            // 
            this.pnlReview.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlReview.Location = new System.Drawing.Point(0, 0);
            this.pnlReview.Name = "pnlReview";
            this.pnlReview.Size = new System.Drawing.Size(1303, 727);
            this.pnlReview.TabIndex = 0;
            // 
            // ImportView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(18F, 45F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(18)))), ((int)(((byte)(16)))), ((int)(((byte)(22)))));
            this.Controls.Add(this.pnlContent);
            this.Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.ForeColor = System.Drawing.Color.Snow;
            this.Margin = new System.Windows.Forms.Padding(5);
            this.Name = "ImportView";
            this.Size = new System.Drawing.Size(1359, 783);
            this.pnlContent.ResumeLayout(false);
            this.tlpImport.ResumeLayout(false);
            this.pnlCardActivity.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private Panel pnlContent;
        private TableLayoutPanel tlpImport;
        private Panel pnlCardActivity;
        private Panel pnlReview;
    }
}
