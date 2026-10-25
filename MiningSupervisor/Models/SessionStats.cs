namespace MiningSupervisor.Models;

/// <summary>
/// In-memory mining statistics for the current run of the program. Never persisted:
/// a restart (or an explicit reset) starts a fresh session.
/// </summary>
public class SessionStats
{
    public double TotalM3 { get; private set; }
    public double ExtraM3 { get; private set; }
    public double LossM3 { get; private set; }

    /// <summary>When the first ore of this session came in; null until then.</summary>
    public DateTime? StartedAt { get; private set; }

    public double ExtraPercent => TotalM3 <= 0 ? 0 : ExtraM3 / TotalM3 * 100;
    public double LossPercent => TotalM3 <= 0 ? 0 : LossM3 / TotalM3 * 100;

    public TimeSpan Length => StartedAt is { } start ? DateTime.Now - start : TimeSpan.Zero;

    public void MarkOreReceived()
    {
        StartedAt ??= DateTime.Now;
    }

    public void AddMined(double m3, bool isCritical)
    {
        MarkOreReceived();
        TotalM3 += m3;
        if (isCritical)
        {
            ExtraM3 += m3;
        }
    }

    public void AddLoss(double m3)
    {
        LossM3 += m3;
    }

    public void Reset()
    {
        TotalM3 = 0;
        ExtraM3 = 0;
        LossM3 = 0;
        StartedAt = null;
    }
}
