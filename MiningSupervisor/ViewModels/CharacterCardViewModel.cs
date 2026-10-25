using System.ComponentModel;
using System.Media;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using MiningSupervisor.Models;

namespace MiningSupervisor.ViewModels;

public enum CargoStatusLevel
{
    Normal,
    Warning,
    Critical
}

public class CharacterCardViewModel : INotifyPropertyChanged
{
    private static readonly Brush NormalColor = CreateFrozenBrush("#4caf50");

    private readonly CharacterEntry _entry;
    private readonly AppState _state;
    private readonly Action _onChanged;
    private CargoStatusLevel _lastStatusLevel = CargoStatusLevel.Normal;

    public CharacterCardViewModel(CharacterEntry entry, AppState state, Action onChanged)
    {
        _entry = entry;
        _state = state;
        _onChanged = onChanged;
        EmptyCommand = new RelayCommand(_ => Empty());
        _lastStatusLevel = ComputeStatusLevel();
    }

    public CharacterEntry Entry => _entry;

    public string Name
    {
        get => _entry.Name;
        set
        {
            if (_entry.Name == value) return;
            _entry.Name = value;
            OnPropertyChanged();
            _onChanged();
        }
    }

    public double CapacityM3
    {
        get => _entry.CapacityM3;
        set
        {
            if (_entry.CapacityM3 == value) return;
            _entry.CapacityM3 = Math.Max(0, value);
            if (_entry.CurrentM3 > _entry.CapacityM3)
            {
                _entry.CurrentM3 = _entry.CapacityM3;
                OnPropertyChanged(nameof(CurrentM3));
            }
            OnPropertyChanged();
            OnPropertyChanged(nameof(PercentFull));
            OnPropertyChanged(nameof(CapacityLabel));
            _onChanged();
            EvaluateStatus();
        }
    }

    public double CurrentM3
    {
        get => _entry.CurrentM3;
        set
        {
            var clamped = Math.Clamp(value, 0, _entry.CapacityM3);
            if (_entry.CurrentM3 == clamped) return;
            _entry.CurrentM3 = clamped;
            OnPropertyChanged();
            OnPropertyChanged(nameof(PercentFull));
            OnPropertyChanged(nameof(CapacityLabel));
            _onChanged();
            EvaluateStatus();
        }
    }

    public double CompressedM3
    {
        get => _entry.CompressedM3;
        set
        {
            var clamped = Math.Clamp(value, 0, _entry.CurrentM3);
            if (_entry.CompressedM3 == clamped) return;
            _entry.CompressedM3 = clamped;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CompressedPercentFull));
            _onChanged();
        }
    }

    public string? UnknownOre
    {
        get => _entry.UnknownOre;
        set
        {
            if (_entry.UnknownOre == value) return;
            _entry.UnknownOre = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasUnknownOre));
        }
    }

    public bool HasUnknownOre => !string.IsNullOrEmpty(UnknownOre);

    public double PercentFull => _entry.CapacityM3 <= 0 ? 0 : Math.Min(100, _entry.CurrentM3 / _entry.CapacityM3 * 100);

    public double CompressedPercentFull => _entry.CapacityM3 <= 0 ? 0 : Math.Min(100, _entry.CompressedM3 / _entry.CapacityM3 * 100);

    public string CapacityLabel => $"{_entry.CurrentM3:N0} / {_entry.CapacityM3:N0} m³";

    public string CapacityShortLabel => $"{FormatShort(_entry.CurrentM3)} / {FormatShort(_entry.CapacityM3)}";

    public string PercentLabel => $"{PercentFull:0}%";

    private static string FormatShort(double m3) =>
        m3 >= 1000 ? $"{m3 / 1000:0.#}k" : $"{m3:0}";

    public Brush CargoColor => ComputeStatusLevel() switch
    {
        CargoStatusLevel.Critical => CreateFrozenBrush(_state.CriticalColorHex),
        CargoStatusLevel.Warning => CreateFrozenBrush(_state.WarningColorHex),
        _ => NormalColor
    };

    public RelayCommand EmptyCommand { get; }

    public void AddMinedVolume(double m3)
    {
        CurrentM3 += m3;
    }

    /// <summary>
    /// Applies a compression event: raw ore volume is removed from cargo and replaced
    /// by the (much smaller) compressed ore volume.
    /// </summary>
    public void ApplyCompression(double consumedRawM3, double producedCompressedM3)
    {
        CurrentM3 = Math.Max(0, CurrentM3 - consumedRawM3 + producedCompressedM3);
        CompressedM3 += producedCompressedM3;
    }

    private void Empty()
    {
        ClearCargo();
    }

    public void ClearCargo()
    {
        CurrentM3 = 0;
        CompressedM3 = 0;
    }

    /// <summary>
    /// Directly overrides the current cargo volume (e.g. bulk "set all cargo").
    /// Composition (raw vs. compressed) is unknown after a manual override, so the
    /// compressed portion is reset.
    /// </summary>
    public void SetCurrentVolume(double m3)
    {
        CompressedM3 = 0;
        CurrentM3 = m3;
    }

    private CargoStatusLevel ComputeStatusLevel()
    {
        if (PercentFull >= _state.CriticalThresholdPercent) return CargoStatusLevel.Critical;
        if (PercentFull >= _state.WarningThresholdPercent) return CargoStatusLevel.Warning;
        return CargoStatusLevel.Normal;
    }

    private void EvaluateStatus()
    {
        var newLevel = ComputeStatusLevel();

        if (newLevel > _lastStatusLevel)
        {
            PlayAlertSound(newLevel);
        }

        _lastStatusLevel = newLevel;
        OnPropertyChanged(nameof(CargoColor));
    }

    private void PlayAlertSound(CargoStatusLevel level)
    {
        try
        {
            if (level == CargoStatusLevel.Critical && _state.CriticalAudioEnabled)
            {
                SystemSounds.Hand.Play();
            }
            else if (level == CargoStatusLevel.Warning && _state.WarningAudioEnabled)
            {
                SystemSounds.Exclamation.Play();
            }
        }
        catch
        {
            // Audio playback is best-effort.
        }
    }

    /// <summary>
    /// Call after global threshold/color settings change so bound colors re-evaluate
    /// without treating it as a new threshold crossing (no sound is played here).
    /// </summary>
    public void RefreshSettingsDependent()
    {
        _lastStatusLevel = ComputeStatusLevel();
        OnPropertyChanged(nameof(CargoColor));
    }

    public void RefreshAll()
    {
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(CapacityM3));
        OnPropertyChanged(nameof(CurrentM3));
        OnPropertyChanged(nameof(CompressedM3));
        OnPropertyChanged(nameof(PercentFull));
        OnPropertyChanged(nameof(CompressedPercentFull));
        OnPropertyChanged(nameof(CapacityLabel));
        OnPropertyChanged(nameof(PercentLabel));
        OnPropertyChanged(nameof(CargoColor));
    }

    private static Brush CreateFrozenBrush(string hex)
    {
        try
        {
            var color = (Color)ColorConverter.ConvertFromString(hex)!;
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }
        catch
        {
            return NormalColor;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        if (name is nameof(CurrentM3) or nameof(CapacityM3))
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PercentLabel)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CapacityShortLabel)));
        }
    }
}
