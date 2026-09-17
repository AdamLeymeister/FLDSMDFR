namespace FLDSMDFR.Core.Models;

public class AnalysisTableRow
{
    public string Sport { get; set; } = string.Empty;

    public string Found { get; set; } = string.Empty;

    public string Word { get; set; } = string.Empty;

    public string File { get; set; } = string.Empty;

    public int LineNumber { get; set; }

    public ReviewDecision Decision { get; set; } = ReviewDecision.Pending;

    public bool IsAccurate
    {
        get => Decision == ReviewDecision.Accurate;
        set => Decision = value ? ReviewDecision.Accurate : ReviewDecision.Pending;
    }

    public int Count { get; set; }

    public int TotalOccurrences { get; set; }
}
