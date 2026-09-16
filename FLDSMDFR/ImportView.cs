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
        ConfigureImportRail();

        btnImportJson.Click += btnImportJson_Click;

        _reviewControl = new AnalysisReviewControl.AnalysisReviewControl
        {
            Dock = DockStyle.Fill
        };

        pnlReview.Controls.Add(_reviewControl);
    }

    private void ConfigureView()
    {
        ModernUi.StylePage(this, pnlContent, tlpImport);

        tlpImport.ColumnStyles[0].SizeType = SizeType.Absolute;
        tlpImport.ColumnStyles[0].Width = 188;
        tlpImport.ColumnStyles[1].SizeType = SizeType.Percent;
        tlpImport.ColumnStyles[1].Width = 100;

        pnlCardActivity.BackColor = DarkMode.Background;
        pnlCardActivity.Margin = new Padding(4, 8, 8, 8);
        pnlCardActivity.Padding = Padding.Empty;
        pnlReview.BackColor = DarkMode.Background;
        pnlReview.Padding = Padding.Empty;
    }

    private void ConfigureImportRail()
    {
        pnlCardOverview.BackColor = DarkMode.Background;
        pnlCardOverview.Margin = new Padding(8, 8, 4, 8);
        pnlCardOverview.Padding = Padding.Empty;

        var card = new RoundedCardPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18),
            BackdropColor = DarkMode.Background,
            FillColor = DarkMode.Surface
        };

        btnImportJson.Parent = null;
        btnImportJson.Dock = DockStyle.Top;
        btnImportJson.Height = 36;
        btnImportJson.Text = "Import JSON";
        ModernUi.StylePrimaryButton(btnImportJson);

        var hint = new Label
        {
            Text = "Load a JSON match export. Review stays on this tab when you leave and come back.",
            Dock = DockStyle.Fill,
            AutoSize = false,
            Font = ModernUi.BodyFont,
            ForeColor = DarkMode.TextDisabled,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 12, 0, 0)
        };

        var caption = new Label
        {
            Text = "SOURCE",
            Dock = DockStyle.Top,
            Height = 18,
            Font = ModernUi.CaptionFont,
            ForeColor = DarkMode.TextDisabled,
            BackColor = Color.Transparent
        };

        card.Controls.Add(hint);
        card.Controls.Add(btnImportJson);
        card.Controls.Add(caption);

        pnlCardOverview.Controls.Add(card);
    }

    private void btnImportJson_Click(object sender, EventArgs e)
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
