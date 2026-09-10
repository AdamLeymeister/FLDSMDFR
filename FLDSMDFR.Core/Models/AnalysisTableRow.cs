using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FLDSMDFR.Core.Models;

public class AnalysisTableRow
{
    public string Sport { get; set; } = string.Empty;

    public string Found { get; set; } = string.Empty;
    public string File { get; set; } = string.Empty;
    public bool IsAccurate { get; set; }

    public int Count { get; set; }

    public int TotalOccurrences { get; set; }
}