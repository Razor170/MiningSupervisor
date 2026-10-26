using System.Text.RegularExpressions;

namespace MiningSupervisor.Services;

public record SurveyRow(string Type, double M3, double Units)
{
    public string M3Label => $"{M3:N0} m³";
    public string UnitsLabel => $"{Units:N0} U";
}

public class SurveyScan
{
    public DateTime Time { get; init; }
    public double TotalM3 { get; init; }
    public IReadOnlyList<SurveyRow> Rows { get; init; } = Array.Empty<SurveyRow>();
}

/// <summary>
/// Parses EVE survey-scan clipboard text and compares consecutive scans to estimate
/// the mining rate and the time until the scanned ore is depleted.
/// </summary>
public class SurveyTracker
{
    private static readonly Regex VolumeRegex = new(@"^\s*(?<n>[\d.,\s  ]+?)\s*m[3³]\s*$", RegexOptions.Compiled);

    public SurveyScan? Previous { get; private set; }
    public SurveyScan? Current { get; private set; }

    /// <summary>Mined volume per minute between the last two scans; null if it can't be derived.</summary>
    public double? RatePerMinute
    {
        get
        {
            if (Previous == null || Current == null)
            {
                return null;
            }

            var minutes = (Current.Time - Previous.Time).TotalMinutes;
            var mined = Previous.TotalM3 - Current.TotalM3;
            return minutes > 0 && mined > 0 ? mined / minutes : null;
        }
    }

    public TimeSpan? TimeToDepletion =>
        Current != null && RatePerMinute is { } rate ? TimeSpan.FromMinutes(Current.TotalM3 / rate) : null;

    public void Reset()
    {
        Previous = null;
        Current = null;
    }

    /// <summary>
    /// Returns true if the text was a survey scan that changed the state. A scan with the same
    /// total as the current one is ignored so the older timestamp stays the rate baseline.
    /// </summary>
    public bool TryIngest(string text, DateTime now)
    {
        var scan = Parse(text, now);
        if (scan == null || (Current != null && Math.Abs(Current.TotalM3 - scan.TotalM3) < 0.5))
        {
            return false;
        }

        Previous = Current;
        Current = scan;
        return true;
    }

    public static SurveyScan? Parse(string text, DateTime now)
    {
        var byType = new Dictionary<string, (double M3, double Units)>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in text.Split('\n'))
        {
            var cols = line.TrimEnd('\r').Split('\t');
            if (cols.Length < 3)
            {
                continue;
            }

            var volumeMatch = VolumeRegex.Match(cols[2]);
            if (!volumeMatch.Success ||
                !TryParseNumber(cols[1], out var units) ||
                !TryParseNumber(volumeMatch.Groups["n"].Value, out var m3))
            {
                continue;
            }

            var type = cols[0].Trim();
            byType.TryGetValue(type, out var existing);
            byType[type] = (existing.M3 + m3, existing.Units + units);
        }

        if (byType.Count == 0)
        {
            return null;
        }

        var rows = byType
            .Select(kvp => new SurveyRow(kvp.Key, kvp.Value.M3, kvp.Value.Units))
            .OrderByDescending(r => r.M3)
            .ToList();

        return new SurveyScan { Time = now, TotalM3 = rows.Sum(r => r.M3), Rows = rows };
    }

    private static bool TryParseNumber(string text, out double value)
    {
        var digits = new string(text.Where(char.IsDigit).ToArray());
        return double.TryParse(digits, out value) && digits.Length > 0;
    }
}
