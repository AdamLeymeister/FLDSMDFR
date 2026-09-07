namespace FLDSMDFR
{
    partial class UtilitiesView
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
            this.tlpUtilities = new System.Windows.Forms.TableLayoutPanel();
            this.pnlCardOverview = new System.Windows.Forms.Panel();
            this.pnlUtilities = new System.Windows.Forms.Panel();
            this.pnlContent.SuspendLayout();
            this.tlpUtilities.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnlContent
            // 
            this.pnlContent.Controls.Add(this.tlpUtilities);
            this.pnlContent.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlContent.Location = new System.Drawing.Point(0, 0);
            this.pnlContent.Name = "pnlContent";
            this.pnlContent.Padding = new System.Windows.Forms.Padding(20);
            this.pnlContent.Size = new System.Drawing.Size(1359, 783);
            this.pnlContent.TabIndex = 0;
            // 
            // tlpUtilities
            // 
            this.tlpUtilities.ColumnCount = 1;
            this.tlpUtilities.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpUtilities.Controls.Add(this.pnlCardOverview, 0, 0);
            this.tlpUtilities.Controls.Add(this.pnlUtilities, 0, 1);
            this.tlpUtilities.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpUtilities.Location = new System.Drawing.Point(20, 20);
            this.tlpUtilities.Name = "tlpUtilities";
            this.tlpUtilities.RowCount = 2;
            this.tlpUtilities.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 11.11111F));
            this.tlpUtilities.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 88.88889F));
            this.tlpUtilities.Size = new System.Drawing.Size(1319, 743);
            this.tlpUtilities.TabIndex = 0;
            // 
            // pnlCardOverview
            // 
            this.pnlCardOverview.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(32)))), ((int)(((byte)(44)))));
            this.pnlCardOverview.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlCardOverview.Location = new System.Drawing.Point(8, 8);
            this.pnlCardOverview.Margin = new System.Windows.Forms.Padding(8);
            this.pnlCardOverview.Name = "pnlCardOverview";
            this.pnlCardOverview.Size = new System.Drawing.Size(1303, 66);
            this.pnlCardOverview.TabIndex = 0;
            // 
            // pnlUtilities
            // 
            this.pnlUtilities.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(32)))), ((int)(((byte)(44)))));
            this.pnlUtilities.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlUtilities.Location = new System.Drawing.Point(8, 90);
            this.pnlUtilities.Margin = new System.Windows.Forms.Padding(8);
            this.pnlUtilities.Name = "pnlUtilities";
            this.pnlUtilities.Size = new System.Drawing.Size(1303, 645);
            this.pnlUtilities.TabIndex = 1;
            // 
            // UtilitiesView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(18F, 45F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(18)))), ((int)(((byte)(16)))), ((int)(((byte)(22)))));
            this.Controls.Add(this.pnlContent);
            this.Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.ForeColor = System.Drawing.Color.Snow;
            this.Margin = new System.Windows.Forms.Padding(5);
            this.Name = "UtilitiesView";
            this.Size = new System.Drawing.Size(1359, 783);
            this.pnlContent.ResumeLayout(false);
            this.tlpUtilities.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private Panel pnlContent;
        private TableLayoutPanel tlpUtilities;
        private Panel pnlCardOverview;
        private Panel pnlUtilities;
    }
}
