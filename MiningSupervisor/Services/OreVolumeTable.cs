using MiningSupervisor.Models;

namespace MiningSupervisor.Services;

public class OreVolumeTable
{
    private const string CompressedPrefix = "Compressed ";

    private static readonly Dictionary<string, double> Defaults = BuildDefaults();

    private readonly AppState _state;

    public OreVolumeTable(AppState state)
    {
        _state = state;
    }

    public bool TryGetVolume(string oreName, out double volumePerUnit)
    {
        oreName = oreName.Trim();

        if (_state.OreVolumeOverrides.TryGetValue(oreName, out volumePerUnit))
        {
            return true;
        }

        if (oreName.StartsWith(CompressedPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var baseName = oreName.Substring(CompressedPrefix.Length);
            if (TryGetVolume(baseName, out var baseVolume))
            {
                volumePerUnit = baseVolume / 100.0;
                return true;
            }
        }

        if (Defaults.TryGetValue(oreName, out volumePerUnit))
        {
            return true;
        }

        volumePerUnit = 0;
        return false;
    }

    private static Dictionary<string, double> BuildDefaults()
    {
        var table = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        void AddGraded(string baseName, double volume)
        {
            table[baseName] = volume;
            table[$"{baseName} II-Grade"] = volume;
            table[$"{baseName} III-Grade"] = volume;
            table[$"{baseName} IV-Grade"] = volume;
        }

        AddGraded("Arkonor", 16.0);
        AddGraded("Bistot", 16.0);
        AddGraded("Crokite", 16.0);
        AddGraded("Dark Ochre", 8.0);
        AddGraded("Ducinium", 16.0);
        AddGraded("Eifyrium", 16.0);
        AddGraded("Gneiss", 5.0);
        AddGraded("Hedbergite", 3.0);
        AddGraded("Jaspet", 2.0);
        AddGraded("Griemeer", 0.8);
        AddGraded("Hezorime", 5.0);
        AddGraded("Plagioclase", 0.35);
        AddGraded("Kernite", 1.2);
        AddGraded("Kylixium", 1.2);
        AddGraded("Scordite", 0.15);
        AddGraded("Spodumain", 16.0);
        AddGraded("Mordunium", 0.1);
        AddGraded("Nocxite", 4.0);
        AddGraded("Omber", 0.6);
        AddGraded("Pyroxeres", 0.3);
        AddGraded("Veldspar", 0.1);
        AddGraded("Ueganite", 5.0);
        AddGraded("Ytirium", 0.6);

        // Mercoxit only has I-III grades.
        table["Mercoxit"] = 40.0;
        table["Mercoxit II-Grade"] = 40.0;
        table["Mercoxit III-Grade"] = 40.0;

        // Ice.
        table["White Glaze"] = 1000.0;
        table["White Glaze IV-Grade"] = 1000.0;
        table["Dark Glitter"] = 1000.0;
        table["Gelidus"] = 1000.0;
        table["Glare Crust"] = 1000.0;
        table["Krystallos"] = 1000.0;

        return table;
    }
}
