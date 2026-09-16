using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using FLDSMDFR.Core.Models;

namespace FLDSMDFR.Core.Services;

public class JsonAnalyzer
{
    public List<AnalysisTableRow> AnalyzeFile(string filePath)
    {
        string json = File.ReadAllText(filePath);
        string baseDirectory = Path.GetDirectoryName(Path.GetFullPath(filePath)) ?? string.Empty;
        List<AnalysisTableRow> rows = AnalyzeJson(json);

        foreach (AnalysisTableRow row in rows)
        {
            row.File = ResolveSourcePath(baseDirectory, row.File);
        }

        return rows;
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

    public List<AnalysisTableRow> AnalyzeJson(string json)
    {
        var data = JsonSerializer.Deserialize<AnalysisData>(
            json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

        if (data == null)
        {
            return new List<AnalysisTableRow>();
        }

        return data.Results
            .SelectMany(fileResult =>
                fileResult.List.Select(term =>
                    new AnalysisTableRow
                    {
                        File = fileResult.File,
                        Sport = term.Sport,
                        Found = term.Found,
                        Decision = term.IsAccurate
                            ? ReviewDecision.Accurate
                            : ReviewDecision.Pending
                    }))
            .ToList();
    }
}
