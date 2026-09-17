using AnalysisReviewControl;
using FLDSMDFR.Core.Models;
using FLDSMDFR.Core.Services;
using FLDSMDFR.Themes;

namespace FLDSMDFR;

public partial class ImportView : UserControl
{
    private readonly AnalysisReviewControl.AnalysisReviewControl _reviewControl;
    private readonly JsonAnalyzer _analyzer = new();
    private string? _sourcePath;
    private List<string> _catalogSports = new();

    public event EventHandler? ReviewChanged;

    public ReviewProgressSnapshot GetProgress()
    {
        return _reviewControl.GetProgress();
    }

    public ImportView()
    {
        InitializeComponent();

        ConfigureView();

        _reviewControl = new AnalysisReviewControl.AnalysisReviewControl
        {
            Dock = DockStyle.Fill
        };
        _reviewControl.ImportClicked += (_, _) => ImportJson();
        _reviewControl.ExportClicked += (_, _) => ExportJson();
        _reviewControl.ReviewChanged += (_, e) => ReviewChanged?.Invoke(this, e);

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
            AnalysisImport imported = _analyzer.AnalyzeFile(openFileDialog.FileName);
            _sourcePath = imported.SourcePath;
            _catalogSports = imported.Sports;
            _reviewControl.LoadData(imported.Rows);
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

    private void ExportJson()
    {
        if (_reviewControl.Rows.Count == 0)
        {
            return;
        }

        string directory = string.IsNullOrWhiteSpace(_sourcePath)
            ? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            : Path.GetDirectoryName(_sourcePath) ?? string.Empty;
        string baseName = string.IsNullOrWhiteSpace(_sourcePath)
            ? "review"
            : Path.GetFileNameWithoutExtension(_sourcePath);

        using SaveFileDialog saveFileDialog = new()
        {
            Title = "Export reviewed JSON",
            Filter = "JSON Files (*.json)|*.json",
            InitialDirectory = directory,
            FileName = $"{baseName}.reviewed.json",
            OverwritePrompt = true
        };

        if (saveFileDialog.ShowDialog() != DialogResult.OK)
        {
            return;
        }

        try
        {
            _analyzer.ExportFile(saveFileDialog.FileName, _catalogSports, _reviewControl.Rows);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to export JSON file.\n\n{ex.Message}",
                "Export Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}
