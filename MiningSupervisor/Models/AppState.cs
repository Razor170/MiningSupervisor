using System.IO;

namespace MiningSupervisor.Models;

public class AppState
{
    private static string DefaultLogDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "EVE", "logs", "Gamelogs");

    public List<CharacterEntry> Characters { get; set; } = new();
    public Dictionary<string, double> OreVolumeOverrides { get; set; } = new();
    public Dictionary<string, LogFileCursor> FileCursors { get; set; } = new();
    public string LogDirectory { get; set; } = DefaultLogDirectory;
    public bool StayOnTop { get; set; } = false;

    public double WarningThresholdPercent { get; set; } = 80;
    public string WarningColorHex { get; set; } = "#FFCC00";
    public bool WarningAudioEnabled { get; set; } = true;

    public double CriticalThresholdPercent { get; set; } = 95;
    public string CriticalColorHex { get; set; } = "#FF3333";
    public bool CriticalAudioEnabled { get; set; } = true;
}
