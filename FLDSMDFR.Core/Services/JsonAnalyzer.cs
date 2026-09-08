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
        var records = JsonSerializer.Deserialize<List<AnalysisRecord>>(
            json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

        if (records == null)
        {
            return new List<AnalysisTableRow>();
        }

        return records
            .Where(x =>
                !string.IsNullOrWhiteSpace(x.Word) &&
                !string.IsNullOrWhiteSpace(x.Result))
            .GroupBy(x => x.Word)
            .SelectMany(wordGroup =>
            {
                int totalWordOccurrences = wordGroup.Count();

                return wordGroup
                    .GroupBy(x => x.Result)
                    .Select(resultGroup => new AnalysisTableRow
                    {
                        Word = wordGroup.Key,
                        Result = resultGroup.Key,
                        Count = resultGroup.Count(),
                        TotalWordOccurrences = totalWordOccurrences
                    });
            })
            .OrderBy(x => x.Word)
            .ThenByDescending(x => x.Count)
            .ToList();
    }
}
