namespace FLDSMDFR.Core.Models;

public sealed class ReviewProgressSnapshot
{
    public int Total { get; init; }

    public int Accurate { get; init; }

    public int Denied { get; init; }

    public int Pending { get; init; }

    public TimeSpan? EstimatedRemaining { get; init; }

    public double PercentComplete =>
        Total <= 0 ? 0 : 100d * (Accurate + Denied) / Total;

    public bool HasData => Total > 0;

    public bool IsComplete => HasData && Pending == 0;
}
