using System.Text.Json;
using FLDSMDFR.Core.Models;

namespace FLDSMDFR.Core.Services;

public sealed class AnalysisImport
{
    public string SourcePath { get; init; } = string.Empty;

    public List<string> Sports { get; init; } = new();

    public List<AnalysisTableRow> Rows { get; init; } = new();
}

public class JsonAnalyzer
{
    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public AnalysisImport AnalyzeFile(string filePath)
    {
        string json = File.ReadAllText(filePath);
        string baseDirectory = Path.GetDirectoryName(Path.GetFullPath(filePath)) ?? string.Empty;
        AnalysisData data = Deserialize(json);

        var rows = new List<AnalysisTableRow>();
        int order = 0;
        foreach (FileResult fileResult in data.Results)
        {
            string originalFile = NormalizeExportPath(fileResult.File);
            string resolved = ResolveSourcePath(baseDirectory, fileResult.File);

            foreach (Term term in fileResult.List)
            {
                rows.Add(new AnalysisTableRow
                {
                    File = resolved,
                    OriginalFile = originalFile,
                    Sport = term.Sport,
                    Found = term.Found,
                    Word = string.IsNullOrWhiteSpace(term.Word) ? term.Found : term.Word,
                    LineNumber = term.Line,
                    ImportOrder = order++,
                    OriginalIsAccurate = term.IsAccurate,
                    Decision = ReadDecision(term)
                });
            }
        }

        return new AnalysisImport
        {
            SourcePath = Path.GetFullPath(filePath),
            Sports = data.Sports
                .Select(sport => sport.SportName)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .ToList(),
            Rows = rows
        };
    }

    public void ExportFile(string filePath, IEnumerable<string> sports, IEnumerable<AnalysisTableRow> rows)
    {
        List<AnalysisTableRow> list = rows.ToList();
        var catalog = sports
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (catalog.Count == 0)
        {
            catalog = list
                .Select(row => row.Sport)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        var data = new AnalysisData
        {
            Sports = catalog.Select(name => new Sport { SportName = name }).ToList(),
            Results = list
                .GroupBy(row => string.IsNullOrWhiteSpace(row.OriginalFile) ? row.File : row.OriginalFile)
                .OrderBy(group => group.Min(row => row.ImportOrder))
                .Select(group =>
                {
                    List<Term> terms = group
                        .OrderBy(row => row.ImportOrder)
                        .Select(ToTerm)
                        .ToList();

                    return new FileResult
                    {
                        File = NormalizeExportPath(group.Key),
                        Count = terms.Count,
                        List = terms
                    };
                })
                .ToList()
        };

        File.WriteAllText(filePath, JsonSerializer.Serialize(data, WriteOptions));
    }

    public List<AnalysisTableRow> AnalyzeJson(string json)
    {
        return AnalyzeFileFromJson(json).Rows;
    }

    private static AnalysisImport AnalyzeFileFromJson(string json)
    {
        AnalysisData data = Deserialize(json);
        var rows = new List<AnalysisTableRow>();
        int order = 0;
        foreach (FileResult fileResult in data.Results)
        {
            foreach (Term term in fileResult.List)
            {
                rows.Add(new AnalysisTableRow
                {
                    File = fileResult.File,
                    OriginalFile = NormalizeExportPath(fileResult.File),
                    Sport = term.Sport,
                    Found = term.Found,
                    Word = string.IsNullOrWhiteSpace(term.Word) ? term.Found : term.Word,
                    LineNumber = term.Line,
                    ImportOrder = order++,
                    OriginalIsAccurate = term.IsAccurate,
                    Decision = ReadDecision(term)
                });
            }
        }

        return new AnalysisImport { Rows = rows };
    }

    private static AnalysisData Deserialize(string json)
    {
        return JsonSerializer.Deserialize<AnalysisData>(json, ReadOptions) ?? new AnalysisData();
    }

    private static Term ToTerm(AnalysisTableRow row)
    {
        return new Term
        {
            Sport = row.Sport,
            Found = row.Found,
            Word = string.IsNullOrWhiteSpace(row.Word) ? row.Found : row.Word,
            Line = row.LineNumber,
            IsAccurate = row.Decision switch
            {
                ReviewDecision.Accurate => true,
                ReviewDecision.Denied => false,
                _ => row.OriginalIsAccurate
            },
            IsReviewed = row.Decision is ReviewDecision.Accurate or ReviewDecision.Denied
        };
    }

    private static ReviewDecision ReadDecision(Term term)
    {
        if (term.IsAccurate)
        {
            return ReviewDecision.Accurate;
        }

        return term.IsReviewed ? ReviewDecision.Denied : ReviewDecision.Pending;
    }

    private static string ResolveSourcePath(string baseDirectory, string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return path;
        }

        string normalized = path.Replace('/', Path.DirectorySeparatorChar);
        if (Path.IsPathRooted(normalized))
        {
            return Path.GetFullPath(normalized);
        }

        return Path.GetFullPath(Path.Combine(baseDirectory, normalized));
    }

    private static string NormalizeExportPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return path;
        }

        return path.Replace('\\', '/');
    }
}
