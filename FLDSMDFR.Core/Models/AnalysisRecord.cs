using System.Text.Json.Serialization;

namespace FLDSMDFR.Core.Models;

public class AnalysisData
{
    public List<Sport> Sports { get; set; } = new();
    public List<FileResult> Results { get; set; } = new();
}

public class Sport
{
    [JsonPropertyName("sport")]
    public string SportName { get; set; } = string.Empty;
}

public class FileResult
{
    public string File { get; set; } = string.Empty;

    public int Count { get; set; }

    public List<Term> List { get; set; } = new();
}

public class Term
{
    public string Sport { get; set; } = string.Empty;

    public string Found { get; set; } = string.Empty;

    public bool IsAccurate { get; set; } = false;
}