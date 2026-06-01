using ComputerCompanion.Models;
using ComputerCompanion.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;

namespace ComputerCompanion.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly Settings _settings;
    private readonly Action<Settings> _onSave;

    public MainWindowSettings MainWindowSettings => _settings.MainWindow;
    public OverlaySettings OverlaySettings => _settings.Overlay;
    public DisplayContentSettings DisplayContentSettings => _settings.DisplayContent;
    public PerformanceSettings PerformanceSettings => _settings.Performance;
    public StartupSettings StartupSettings => _settings.Startup;

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

    [ObservableProperty]
    private bool _enableOverlay = true;

    [ObservableProperty]
    private bool _overlayAlwaysOnTop = true;

    [ObservableProperty]
    private int _overlayFontSize = 16;

    [ObservableProperty]
    private string _overlayTextColor = "#76B900";

    [ObservableProperty]
    private OverlayPosition _overlayPosition = OverlayPosition.TopRight;

    [ObservableProperty]
    private bool _overlayShowFPS = true;

    [ObservableProperty]
    private bool _overlayShowGpu = true;

    [ObservableProperty]
    private bool _overlayShowCpu = true;

    [ObservableProperty]
    private bool _overlayShowMemory = true;

    [ObservableProperty]
    private bool _overlayShowLatency = true;

    [ObservableProperty]
    private bool _autoStart = false;

    [ObservableProperty]
    private bool _startMinimized = false;

    public SettingsViewModel(Settings settings, Action<Settings> onSave)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _onSave = onSave;

        LoadSettings();
    }

    private void LoadSettings()
    {
        LayoutMode = MainWindowSettings.LayoutMode;
        TextColor = MainWindowSettings.TextColor;
        BackgroundColor = MainWindowSettings.BackgroundColor;
        BackgroundOpacity = MainWindowSettings.BackgroundOpacity;
        FontSize = MainWindowSettings.FontSize;
        RefreshInterval = PerformanceSettings.RefreshInterval;
        GameMode = PerformanceSettings.GameMode;
        GameModeRefreshInterval = PerformanceSettings.GameModeRefreshInterval;
        ShowCpu = DisplayContentSettings.ShowCpu;
        ShowGpu = DisplayContentSettings.ShowGpu;
        ShowMemory = DisplayContentSettings.ShowMemory;
        ShowNetwork = DisplayContentSettings.ShowNetwork;
        ShowDisk = DisplayContentSettings.ShowDisk;
        ShowBattery = DisplayContentSettings.ShowBattery;
        
        EnableOverlay = OverlaySettings.EnableOverlay;
        OverlayAlwaysOnTop = OverlaySettings.OverlayAlwaysOnTop;
        OverlayFontSize = OverlaySettings.OverlayFontSize;
        OverlayTextColor = OverlaySettings.OverlayTextColor;
        OverlayPosition = OverlaySettings.OverlayPosition;
        OverlayShowFPS = OverlaySettings.OverlayShowFPS;
        OverlayShowGpu = OverlaySettings.OverlayShowGpu;
        OverlayShowCpu = OverlaySettings.OverlayShowCpu;
        OverlayShowMemory = OverlaySettings.OverlayShowMemory;
        OverlayShowLatency = OverlaySettings.OverlayShowLatency;
        
        AutoStart = StartupSettings.AutoStart;
        StartMinimized = StartupSettings.StartMinimized;
    }

    [RelayCommand]
    public void Save()
    {
        MainWindowSettings.LayoutMode = LayoutMode;
        MainWindowSettings.TextColor = TextColor;
        MainWindowSettings.BackgroundColor = BackgroundColor;
        MainWindowSettings.BackgroundOpacity = BackgroundOpacity;
        MainWindowSettings.FontSize = FontSize;
        PerformanceSettings.RefreshInterval = RefreshInterval;
        PerformanceSettings.GameMode = GameMode;
        PerformanceSettings.GameModeRefreshInterval = GameModeRefreshInterval;
        DisplayContentSettings.ShowCpu = ShowCpu;
        DisplayContentSettings.ShowGpu = ShowGpu;
        DisplayContentSettings.ShowMemory = ShowMemory;
        DisplayContentSettings.ShowNetwork = ShowNetwork;
        DisplayContentSettings.ShowDisk = ShowDisk;
        DisplayContentSettings.ShowBattery = ShowBattery;
        
        OverlaySettings.EnableOverlay = EnableOverlay;
        OverlaySettings.OverlayAlwaysOnTop = OverlayAlwaysOnTop;
        OverlaySettings.OverlayFontSize = OverlayFontSize;
        OverlaySettings.OverlayTextColor = OverlayTextColor;
        OverlaySettings.OverlayPosition = OverlayPosition;
        OverlaySettings.OverlayShowFPS = OverlayShowFPS;
        OverlaySettings.OverlayShowGpu = OverlayShowGpu;
        OverlaySettings.OverlayShowCpu = OverlayShowCpu;
        OverlaySettings.OverlayShowMemory = OverlayShowMemory;
        OverlaySettings.OverlayShowLatency = OverlayShowLatency;
        
        StartupSettings.AutoStart = AutoStart;
        StartupSettings.StartMinimized = StartMinimized;

        _onSave?.Invoke(_settings);
    }

    [RelayCommand]
    public void ResetToDefaults()
    {
        _settings.ResetToDefaults();
        LoadSettings();
    }
}