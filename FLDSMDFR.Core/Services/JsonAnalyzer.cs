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

        return AnalyzeJson(json);
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
