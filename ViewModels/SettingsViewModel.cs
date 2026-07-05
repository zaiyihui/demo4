using ComputerCompanion.Models;
using ComputerCompanion.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;

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

    [ObservableProperty]
    private bool _autoBackupEnabled = true;

    [ObservableProperty]
    private int _autoBackupIntervalHours = 24;

    [ObservableProperty]
    private bool _differentialBackupEnabled = false;

    [ObservableProperty]
    private string? _validationError;

    public SettingsViewModel(Settings settings, Action<Settings> onSave)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _onSave = onSave;

        LoadSettings();
    }

    private void LoadSettings()
    {
        try
        {
            LayoutMode = MainWindowSettings.LayoutMode;
            TextColor = MainWindowSettings.TextColor;
            BackgroundColor = MainWindowSettings.BackgroundColor;
            BackgroundOpacity = MainWindowSettings.BackgroundOpacity;
            FontSize = MainWindowSettings.FontSize;
            RefreshInterval = PerformanceSettings.RefreshInterval;
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

            AutoBackupEnabled = PerformanceSettings.AutoBackupEnabled;
            AutoBackupIntervalHours = PerformanceSettings.AutoBackupIntervalHours;
            DifferentialBackupEnabled = PerformanceSettings.DifferentialBackupEnabled;

            ValidationError = null;
        }
        catch (Exception ex)
        {
            ValidationError = $"加载设置失败: {ex.Message}";
        }
    }

    private bool ValidateSettings()
    {
        var errors = new List<string>();

        if (RefreshInterval < 200)
            errors.Add("刷新间隔不能小于200毫秒");
        if (RefreshInterval > 60000)
            errors.Add("刷新间隔不能大于60秒");

        if (FontSize < 8)
            errors.Add("字体大小不能小于8");
        if (FontSize > 48)
            errors.Add("字体大小不能大于48");

        if (OverlayFontSize < 8)
            errors.Add("悬浮窗字体大小不能小于8");
        if (OverlayFontSize > 32)
            errors.Add("悬浮窗字体大小不能大于32");

        if (AutoBackupIntervalHours < 1)
            errors.Add("自动备份间隔不能小于1小时");
        if (AutoBackupIntervalHours > 168)
            errors.Add("自动备份间隔不能大于7天");

        ValidationError = errors.Count > 0 ? string.Join("; ", errors) : null;
        return errors.Count == 0;
    }

    [RelayCommand]
    public void Save()
    {
        try
        {
            if (!ValidateSettings())
                return;

            MainWindowSettings.LayoutMode = LayoutMode;
            MainWindowSettings.TextColor = TextColor;
            MainWindowSettings.BackgroundColor = BackgroundColor;
            MainWindowSettings.BackgroundOpacity = BackgroundOpacity;
            MainWindowSettings.FontSize = FontSize;
            PerformanceSettings.RefreshInterval = RefreshInterval;
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

            PerformanceSettings.AutoBackupEnabled = AutoBackupEnabled;
            PerformanceSettings.AutoBackupIntervalHours = AutoBackupIntervalHours;
            PerformanceSettings.DifferentialBackupEnabled = DifferentialBackupEnabled;

            _onSave?.Invoke(_settings);
            ValidationError = null;
        }
        catch (Exception ex)
        {
            ValidationError = $"保存设置失败: {ex.Message}";
        }
    }

    [RelayCommand]
    public void ResetToDefaults()
    {
        try
        {
            _settings.ResetToDefaults();
            LoadSettings();
            ValidationError = null;
        }
        catch (Exception ex)
        {
            ValidationError = $"重置设置失败: {ex.Message}";
        }
    }
}