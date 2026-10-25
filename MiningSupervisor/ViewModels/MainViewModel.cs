using System.Collections.ObjectModel;
using MiningSupervisor.Models;
using MiningSupervisor.Services;

namespace MiningSupervisor.ViewModels;

public class MainViewModel
{
    private readonly DataStore _dataStore;
    private readonly OreVolumeTable _oreVolumeTable;
    private readonly GameLogMonitor _monitor;

    public ObservableCollection<CharacterCardViewModel> Characters { get; } = new();

    public RelayCommand AddCharacterCommand { get; }

    public MainViewModel()
    {
        _dataStore = new DataStore();
        _oreVolumeTable = new OreVolumeTable(_dataStore.State);
        _monitor = new GameLogMonitor(_dataStore.State);
        _monitor.OreMined += OnOreMined;
        _monitor.OreCompressed += OnOreCompressed;

        foreach (var entry in _dataStore.State.Characters)
        {
            Characters.Add(new CharacterCardViewModel(entry, _dataStore.State, _dataStore.RequestSave));
        }

        AddCharacterCommand = new RelayCommand(_ => AddCharacter());

        _monitor.Start();
    }

    public void AddCharacter(string name = "New Character", double capacityM3 = 11500)
    {
        var entry = new CharacterEntry { Name = name, CapacityM3 = capacityM3, CurrentM3 = 0 };
        _dataStore.State.Characters.Add(entry);
        Characters.Add(new CharacterCardViewModel(entry, _dataStore.State, _dataStore.RequestSave));
        _dataStore.RequestSave();
    }

    private void AddCharacter()
    {
        AddCharacter();
    }

    public IReadOnlyList<string> GetAvailableCharacterNames() => _monitor.GetKnownListenerNames();

    public AppState State => _dataStore.State;

    /// <summary>
    /// Call after settings (thresholds/colors/audio) are edited so bound colors update
    /// immediately, without treating the change itself as a threshold crossing.
    /// </summary>
    public void NotifySettingsChanged()
    {
        foreach (var vm in Characters)
        {
            vm.RefreshSettingsDependent();
        }

        _dataStore.RequestSave();
    }

    public bool StayOnTop
    {
        get => _dataStore.State.StayOnTop;
        set
        {
            _dataStore.State.StayOnTop = value;
            _dataStore.RequestSave();
        }
    }

    public void RemoveCharacter(CharacterCardViewModel vm)
    {
        _dataStore.State.Characters.Remove(vm.Entry);
        Characters.Remove(vm);
        _dataStore.RequestSave();
    }

    private void OnOreMined(object? sender, OreMinedEventArgs e)
    {
        var vm = Characters.FirstOrDefault(c => string.Equals(c.Name, e.CharacterName, StringComparison.OrdinalIgnoreCase));
        if (vm == null)
        {
            return;
        }

        if (_oreVolumeTable.TryGetVolume(e.OreName, out var volumePerUnit))
        {
            vm.UnknownOre = null;
            vm.AddMinedVolume(e.Quantity * volumePerUnit);
        }
        else
        {
            vm.UnknownOre = e.OreName;
        }

        _dataStore.RequestSave();
    }

    private void OnOreCompressed(object? sender, OreCompressedEventArgs e)
    {
        var vm = Characters.FirstOrDefault(c => string.Equals(c.Name, e.CharacterName, StringComparison.OrdinalIgnoreCase));
        if (vm == null)
        {
            return;
        }

        // Compression preserves unit count (N raw units -> N compressed units); only the
        // volume per unit shrinks by a factor of 100.
        if (_oreVolumeTable.TryGetVolume(e.OreName, out var rawVolumePerUnit))
        {
            var consumedRawM3 = e.CompressedQuantity * rawVolumePerUnit;
            var producedCompressedM3 = consumedRawM3 / 100.0;
            vm.ApplyCompression(consumedRawM3, producedCompressedM3);
            _dataStore.RequestSave();
        }
    }

    public void ClearAllCargo()
    {
        foreach (var vm in Characters)
        {
            vm.ClearCargo();
        }

        _dataStore.RequestSave();
    }

    public void SetAllCargo(double m3)
    {
        foreach (var vm in Characters)
        {
            vm.SetCurrentVolume(m3);
        }

        _dataStore.RequestSave();
    }

    public void SaveNow() => _dataStore.SaveNow();
}
