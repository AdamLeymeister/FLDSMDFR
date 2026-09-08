using System;
using System.Drawing;
using System.Windows.Forms;

using FLDSMDFR.Core.Services;
using FLDSMDFR.Themes;

namespace FLDSMDFR;

public partial class ImportView : UserControl
{
    public ImportView()
    {
        InitializeComponent();

        ConfigureView();
        ConfigureImportButton();

        btnImportJson.Click += btnImportJson_Click;
    }

    private void ConfigureView()
    {
        BackColor = DarkMode.Background;
        ForeColor = DarkMode.TextPrimary;
    }

    private void ConfigureImportButton()
    {
        btnImportJson.Text = "Import";

        btnImportJson.FlatStyle = FlatStyle.Flat;
        btnImportJson.FlatAppearance.BorderSize = 0;

        // Normal
        btnImportJson.BackColor = DarkMode.Primary;
        btnImportJson.ForeColor = DarkMode.Background;

        // Hover
        btnImportJson.FlatAppearance.MouseOverBackColor =
            DarkMode.PrimaryHover;

        // Pressed
        btnImportJson.FlatAppearance.MouseDownBackColor =
            DarkMode.PrimaryMuted;

        btnImportJson.Font = new Font(
            "Segoe UI",
            10f,
            FontStyle.Bold);

        btnImportJson.Cursor = Cursors.Hand;
        btnImportJson.TabStop = false;

        btnImportJson.UseVisualStyleBackColor = false;
    }

    private void btnImportJson_Click(object sender, EventArgs e)
    {
        using OpenFileDialog openFileDialog = new OpenFileDialog();

        openFileDialog.Title = "Select JSON File";
        openFileDialog.Filter = "JSON Files (*.json)|*.json";
        openFileDialog.Multiselect = false;

        if (openFileDialog.ShowDialog() != DialogResult.OK)
        {
            return;
        }

        try
        {
            var analyzer = new JsonAnalyzer();

            var tableData = analyzer.AnalyzeFile(
                openFileDialog.FileName);

            // We'll bind tableData to the table here.
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to import JSON file.\n\n{ex.Message}",
                "Import Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}
