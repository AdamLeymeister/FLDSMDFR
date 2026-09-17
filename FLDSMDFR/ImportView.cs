using AnalysisReviewControl;
using FLDSMDFR.Core.Services;
using FLDSMDFR.Themes;

namespace FLDSMDFR;

public partial class ImportView : UserControl
{
    private readonly AnalysisReviewControl.AnalysisReviewControl _reviewControl;

    public ImportView()
    {
        InitializeComponent();

        ConfigureView();

        _reviewControl = new AnalysisReviewControl.AnalysisReviewControl
        {
            Dock = DockStyle.Fill
        };
        _reviewControl.ImportClicked += (_, _) => ImportJson();

        pnlReview.Controls.Add(_reviewControl);
    }

    private void ConfigureView()
    {
        ModernUi.StylePage(this, pnlContent, tlpImport);

        tlpImport.ColumnStyles.Clear();
        tlpImport.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        tlpImport.ColumnCount = 1;

        pnlCardActivity.BackColor = DarkMode.Background;
        pnlCardActivity.Margin = new Padding(8);
        pnlCardActivity.Padding = Padding.Empty;
        pnlReview.BackColor = DarkMode.Background;
        pnlReview.Padding = Padding.Empty;
    }

    private void ImportJson()
    {
        using OpenFileDialog openFileDialog = new()
        {
            Title = "Select JSON File",
            Filter = "JSON Files (*.json)|*.json",
            Multiselect = false
        };

        if (openFileDialog.ShowDialog() != DialogResult.OK)
        {
            return;
        }

        try
        {
            var analyzer = new JsonAnalyzer();
            var rows = analyzer.AnalyzeFile(openFileDialog.FileName);
            _reviewControl.LoadData(rows);
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
