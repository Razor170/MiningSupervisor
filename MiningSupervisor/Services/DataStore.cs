using System.IO;
using System.Text.Json;
using MiningSupervisor.Models;

namespace MiningSupervisor.Services;

public class DataStore
{
    private static readonly string DirPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MiningSupervisor");

    private static readonly string FilePath = Path.Combine(DirPath, "data.json");

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly System.Windows.Threading.DispatcherTimer _saveTimer;
    private bool _saveScheduled;

    public AppState State { get; private set; }

    public DataStore()
    {
        State = Load();
        _saveTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _saveTimer.Tick += (_, _) =>
        {
            _saveTimer.Stop();
            if (_saveScheduled)
            {
                _saveScheduled = false;
                SaveNow();
            }
        };
    }

    public void RequestSave()
    {
        _saveScheduled = true;
        if (!_saveTimer.IsEnabled)
        {
            _saveTimer.Start();
        }
    }

    public void SaveNow()
    {
        try
        {
            Directory.CreateDirectory(DirPath);
            var json = JsonSerializer.Serialize(State, JsonOptions);
            File.WriteAllText(FilePath, json);
        }
        catch
        {
            // Best-effort persistence; a failed save shouldn't crash the app.
        }
    }

    private static AppState Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var json = File.ReadAllText(FilePath);
                var state = JsonSerializer.Deserialize<AppState>(json);
                if (state != null)
                {
                    return state;
                }
            }
        }
        catch
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    File.Copy(FilePath, FilePath + ".bak", overwrite: true);
                }
            }
            catch
            {
                // ignore
            }
        }

        return new AppState();
    }
}
