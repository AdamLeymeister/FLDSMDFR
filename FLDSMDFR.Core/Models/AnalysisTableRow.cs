using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FLDSMDFR.Core.Models;

public class AnalysisTableRow
{
    public string Word { get; set; } = string.Empty;

    public string Result { get; set; } = string.Empty;

    public int Count { get; set; }

    public int TotalWordOccurrences { get; set; }
}
