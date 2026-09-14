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
