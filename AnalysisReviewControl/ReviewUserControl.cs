using System.Drawing;

namespace AnalysisReviewControl;

public class ReviewUserControl : UserControl
{
    private readonly List<Font> _ownedFonts = new();

    protected Font CreateOwnedFont(
        string familyName,
        float emSize,
        FontStyle style = FontStyle.Regular)
    {
        Font font = new Font(familyName, emSize, style);
        _ownedFonts.Add(font);
        return font;
    }

    protected static void DisposeChildren(Control parent)
    {
        while (parent.Controls.Count > 0)
        {
            parent.Controls[0].Dispose();
        }
    }

    /// <summary>
    /// Three-state checkboxes cycle Checked → Indeterminate on click.
    /// Snap that to Unchecked so one click from "all accurate" clears all.
    /// Returns false when the change was programmatic.
    /// </summary>
    protected static bool TryGetUserBulkAccuracy(
        CheckBox checkbox,
        ref bool updatingCheckbox,
        out bool isAccurate)
    {
        isAccurate = false;

        if (updatingCheckbox)
        {
            return false;
        }

        if (checkbox.CheckState == CheckState.Indeterminate)
        {
            updatingCheckbox = true;
            checkbox.CheckState = CheckState.Unchecked;
            updatingCheckbox = false;
        }

        isAccurate = checkbox.CheckState == CheckState.Checked;
        return true;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            foreach (Font font in _ownedFonts)
            {
                font.Dispose();
            }

            _ownedFonts.Clear();
        }
    }
}
