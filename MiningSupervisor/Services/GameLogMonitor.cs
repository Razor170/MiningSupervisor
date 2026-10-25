using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Threading;
using MiningSupervisor.Models;

namespace MiningSupervisor.Services;

public class OreMinedEventArgs : EventArgs
{
    public string CharacterName { get; init; } = "";
    public string OreName { get; init; } = "";
    public double Quantity { get; init; }
}

public class OreCompressedEventArgs : EventArgs
{
    public string CharacterName { get; init; } = "";
    public string OreName { get; init; } = "";
    public double CompressedQuantity { get; init; }
}

public class GameLogMonitor
{
    private static readonly Regex ListenerRegex = new(@"Listener:\s*(?<name>.+?)\s*$", RegexOptions.Compiled);

    private static readonly Regex MiningRegex = new(
        @"\(mining\).*?You mined.*?>(?<qty>[\d,]+)<.*?units of.*?>(?<ore>[^<]+?)\s*$",
        RegexOptions.Compiled);

    private static readonly Regex CompressionRegex = new(
        @"Successfully compressed (?<ore>.+?) into (?<qty>[\d,]+) Compressed \k<ore>\.?\s*$",
        RegexOptions.Compiled);

    private readonly AppState _state;
    private readonly DispatcherTimer _timer;

    // filePath -> listener name, cached for the lifetime of the process to avoid re-reading headers.
    private readonly Dictionary<string, string> _listenerCache = new();

    public event EventHandler<OreMinedEventArgs>? OreMined;
    public event EventHandler<OreCompressedEventArgs>? OreCompressed;

    public GameLogMonitor(AppState state)
    {
        _state = state;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _timer.Tick += (_, _) => Tick();
    }

    public void Start() => _timer.Start();

    public void Stop() => _timer.Stop();

    /// <summary>
    /// Scans the log directory for character names (from each file's "Listener:" header)
    /// so the UI can offer them in a dropdown, then returns the distinct set found so far.
    /// </summary>
    public IReadOnlyList<string> GetKnownListenerNames()
    {
        ScanListeners();
        return _listenerCache.Values.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(n => n).ToList();
    }

    private void ScanListeners()
    {
        var dir = _state.LogDirectory;
        if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir))
        {
            return;
        }

        string[] files;
        try
        {
            files = Directory.GetFiles(dir, "*.txt");
        }
        catch
        {
            return;
        }

        foreach (var file in files)
        {
            if (_listenerCache.ContainsKey(file))
            {
                continue;
            }

            var listener = TryReadListener(file);
            if (listener != null)
            {
                _listenerCache[file] = listener;
            }
        }
    }

    private void Tick()
    {
        ScanListeners();

        var characterNames = new HashSet<string>(_state.Characters.Select(c => c.Name), StringComparer.OrdinalIgnoreCase);

        // Newest file (by name, which is a timestamp) per listener among known characters.
        var latestByListener = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var kvp in _listenerCache)
        {
            if (!characterNames.Contains(kvp.Value))
            {
                continue;
            }

            if (!latestByListener.TryGetValue(kvp.Value, out var current) ||
                string.CompareOrdinal(Path.GetFileName(kvp.Key), Path.GetFileName(current)) > 0)
            {
                latestByListener[kvp.Value] = kvp.Key;
            }
        }

        foreach (var character in _state.Characters)
        {
            if (!latestByListener.TryGetValue(character.Name, out var filePath))
            {
                continue;
            }

            if (character.TrackedFilePath != filePath)
            {
                character.TrackedFilePath = filePath;
                if (!_state.FileCursors.ContainsKey(filePath))
                {
                    // Newly discovered file: skip its existing history and only track mining
                    // from this point forward, rather than backfilling the whole session.
                    long startOffset = 0;
                    try
                    {
                        startOffset = new FileInfo(filePath).Length;
                    }
                    catch
                    {
                        // ignore
                    }

                    _state.FileCursors[filePath] = new LogFileCursor { ListenerName = character.Name, ByteOffset = startOffset };
                }
            }

            TailFile(character, filePath);
        }
    }

    private void TailFile(CharacterEntry character, string filePath)
    {
        if (!_state.FileCursors.TryGetValue(filePath, out var cursor))
        {
            cursor = new LogFileCursor { ListenerName = character.Name, ByteOffset = 0 };
            _state.FileCursors[filePath] = cursor;
        }

        try
        {
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            if (cursor.ByteOffset > fs.Length)
            {
                // File was truncated/replaced; restart from the beginning.
                cursor.ByteOffset = 0;
            }

            fs.Seek(cursor.ByteOffset, SeekOrigin.Begin);
            using var reader = new StreamReader(fs);
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                ProcessLine(character, line);
            }

            cursor.ByteOffset = fs.Position;
        }
        catch
        {
            // File may be locked/rotating; try again next tick.
        }
    }

    private void ProcessLine(CharacterEntry character, string line)
    {
        var miningMatch = MiningRegex.Match(line);
        if (miningMatch.Success)
        {
            var qtyText = miningMatch.Groups["qty"].Value.Replace(",", "");
            var oreName = miningMatch.Groups["ore"].Value.Trim();

            if (double.TryParse(qtyText, out var qty))
            {
                OreMined?.Invoke(this, new OreMinedEventArgs
                {
                    CharacterName = character.Name,
                    OreName = oreName,
                    Quantity = qty
                });
            }

            return;
        }

        var compressionMatch = CompressionRegex.Match(line);
        if (compressionMatch.Success)
        {
            var qtyText = compressionMatch.Groups["qty"].Value.Replace(",", "");
            var oreName = compressionMatch.Groups["ore"].Value.Trim();

            if (double.TryParse(qtyText, out var qty))
            {
                OreCompressed?.Invoke(this, new OreCompressedEventArgs
                {
                    CharacterName = character.Name,
                    OreName = oreName,
                    CompressedQuantity = qty
                });
            }
        }
    }

    private static string? TryReadListener(string filePath)
    {
        try
        {
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(fs);
            for (var i = 0; i < 6; i++)
            {
                var line = reader.ReadLine();
                if (line == null)
                {
                    break;
                }

                var match = ListenerRegex.Match(line);
                if (match.Success)
                {
                    return match.Groups["name"].Value;
                }
            }
        }
        catch
        {
            // ignore - file may be locked or too short
        }

        return null;
    }
}
