using Themes;

namespace AnalysisReviewControl;

internal sealed class FocusReviewPanel : Panel
{
    private readonly Label _caption = new();
    private readonly Label _progress = new();
    private readonly Label _word = new();
    private readonly SourcePreviewPanel _preview = new();

    public FocusReviewPanel()
    {
        SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
        Dock = DockStyle.Fill;
        BackColor = DarkMode.Surface;
        Padding = new Padding(28, 22, 28, 22);
        Visible = false;

        _caption.AutoSize = false;
        _caption.Height = 18;
        _caption.Dock = DockStyle.Top;
        _caption.Text = "FOCUS REVIEW";
        _caption.Font = new Font("Segoe UI", 7.5f, FontStyle.Bold);
        _caption.ForeColor = DarkMode.TextDisabled;
        _caption.BackColor = DarkMode.Surface;

        _progress.AutoSize = false;
        _progress.Height = 22;
        _progress.Dock = DockStyle.Bottom;
        _progress.TextAlign = ContentAlignment.MiddleLeft;
        _progress.Font = new Font("Segoe UI", 9f);
        _progress.ForeColor = DarkMode.TextSecondary;
        _progress.BackColor = DarkMode.Surface;

        _word.AutoSize = false;
        _word.Height = 36;
        _word.Dock = DockStyle.Top;
        _word.Font = new Font("Segoe UI", 18f, FontStyle.Bold);
        _word.ForeColor = DarkMode.Primary;
        _word.BackColor = DarkMode.Surface;
        _word.Padding = new Padding(0, 4, 0, 0);

        Controls.Add(_preview);
        Controls.Add(_progress);
        Controls.Add(_word);
        Controls.Add(_caption);
    }

    public void ShowEmpty(string message)
    {
        _word.Text = "Review complete";
        _progress.Text = message;
        _preview.ShowEmpty(message);
    }

    public void ShowHit(
        string word,
        string fileName,
        int lineNumber,
        string reason,
        string highlight,
        string[] lines,
        string progress)
    {
        _word.Text = string.IsNullOrWhiteSpace(word) ? "(hit)" : word;
        _progress.Text = progress;
        _preview.ShowHit(fileName, lineNumber, reason, highlight, lines);
    }
}
