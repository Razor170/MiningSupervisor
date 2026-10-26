using System.IO;

namespace MiningSupervisor.Models;

public class WindowPlacement
{
    public double Left { get; set; }
    public double Top { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
}

public class AppState
{
    public Dictionary<string, WindowPlacement> WindowPlacements { get; set; } = new();

    private static string DefaultLogDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "EVE", "logs", "Gamelogs");

    public List<CharacterEntry> Characters { get; set; } = new();
    public Dictionary<string, double> OreVolumeOverrides { get; set; } = new();
    public Dictionary<string, LogFileCursor> FileCursors { get; set; } = new();
    public string LogDirectory { get; set; } = DefaultLogDirectory;
    public bool StayOnTop { get; set; } = false;
    public double PollIntervalSeconds { get; set; } = 2;

    public double WarningThresholdPercent { get; set; } = 80;
    public string WarningColorHex { get; set; } = "#FFCC00";
    public bool WarningAudioEnabled { get; set; } = true;

    public double CriticalThresholdPercent { get; set; } = 95;
    public string CriticalColorHex { get; set; } = "#FF3333";
    public bool CriticalAudioEnabled { get; set; } = true;
}
