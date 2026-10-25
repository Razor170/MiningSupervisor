namespace MiningSupervisor.Models;

public class CharacterEntry
{
    public string Name { get; set; } = "";
    public double CapacityM3 { get; set; } = 11500;
    public double CurrentM3 { get; set; } = 0;
    public double CompressedM3 { get; set; } = 0;

    public string? TrackedFilePath { get; set; }
    public string? UnknownOre { get; set; }
}
