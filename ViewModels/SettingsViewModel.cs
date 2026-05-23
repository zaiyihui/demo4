using ComputerCompanion.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;

namespace ComputerCompanion.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly Settings _settings;
    private readonly Action<Settings> _onSave;

    [ObservableProperty]
    private LayoutMode _layoutMode = LayoutMode.Vertical;

    [ObservableProperty]
    private string _textColor = "#FFFFFF";

    [ObservableProperty]
    private string _backgroundColor = "#1a1a2eea";

    [ObservableProperty]
    private double _backgroundOpacity = 0.9;

    [ObservableProperty]
    private int _fontSize = 14;

    [ObservableProperty]
    private int _refreshInterval = 1000;

    [ObservableProperty]
    private bool _gameMode;

    [ObservableProperty]
    private int _gameModeRefreshInterval = 3000;

    [ObservableProperty]
    private bool _showCpu = true;

    [ObservableProperty]
    private bool _showGpu = true;

    [ObservableProperty]
    private bool _showMemory = true;

    [ObservableProperty]
    private bool _showNetwork = true;

    [ObservableProperty]
    private bool _showDisk = true;

    [ObservableProperty]
    private bool _showBattery = true;

    public SettingsViewModel(Settings settings, Action<Settings> onSave)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _onSave = onSave;

        LoadSettings();
    }

    private void LoadSettings()
    {
        LayoutMode = _settings.LayoutMode;
        TextColor = _settings.TextColor;
        BackgroundColor = _settings.BackgroundColor;
        BackgroundOpacity = _settings.BackgroundOpacity;
        FontSize = _settings.FontSize;
        RefreshInterval = _settings.RefreshInterval;
        GameMode = _settings.GameMode;
        GameModeRefreshInterval = _settings.GameModeRefreshInterval;
        ShowCpu = _settings.ShowCpu;
        ShowGpu = _settings.ShowGpu;
        ShowMemory = _settings.ShowMemory;
        ShowNetwork = _settings.ShowNetwork;
        ShowDisk = _settings.ShowDisk;
        ShowBattery = _settings.ShowBattery;
    }

    [RelayCommand]
    public void Save()
    {
        _settings.LayoutMode = LayoutMode;
        _settings.TextColor = TextColor;
        _settings.BackgroundColor = BackgroundColor;
        _settings.BackgroundOpacity = BackgroundOpacity;
        _settings.FontSize = FontSize;
        _settings.RefreshInterval = RefreshInterval;
        _settings.GameMode = GameMode;
        _settings.GameModeRefreshInterval = GameModeRefreshInterval;
        _settings.ShowCpu = ShowCpu;
        _settings.ShowGpu = ShowGpu;
        _settings.ShowMemory = ShowMemory;
        _settings.ShowNetwork = ShowNetwork;
        _settings.ShowDisk = ShowDisk;
        _settings.ShowBattery = ShowBattery;

        _onSave?.Invoke(_settings);
    }

    [RelayCommand]
    public void ResetToDefaults()
    {
        LayoutMode = LayoutMode.Vertical;
        TextColor = "#FFFFFF";
        BackgroundColor = "#1a1a2eea";
        BackgroundOpacity = 0.9;
        FontSize = 14;
        RefreshInterval = 1000;
        GameMode = false;
        GameModeRefreshInterval = 3000;
        ShowCpu = true;
        ShowGpu = true;
        ShowMemory = true;
        ShowNetwork = true;
        ShowDisk = true;
        ShowBattery = true;
    }
}
