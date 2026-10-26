namespace MiningSupervisor.Models;

public class CharacterEntry
{
    public string Name { get; set; } = "";
    public double CapacityM3 { get; set; } = 11500;
    public double CurrentM3 { get; set; } = 0;
    public double CompressedM3 { get; set; } = 0;

    /// <summary>Hidden characters are still tracked, just not shown in the main window.</summary>
    public bool Hidden { get; set; }

    public string? TrackedFilePath { get; set; }
    public string? UnknownOre { get; set; }
}
