using Newtonsoft.Json;
using System;
using System.IO;

namespace ComputerCompanion.Services;

public class Settings
{
    public LayoutMode LayoutMode { get; set; } = LayoutMode.Vertical;
    public string TextColor { get; set; } = "#FFFFFF";
    public string BackgroundColor { get; set; } = "#1a1a2eea";
    public double BackgroundOpacity { get; set; } = 0.9;
    public int FontSize { get; set; } = 14;
    public int RefreshInterval { get; set; } = 1000;
    public bool GameMode { get; set; } = false;
    public int GameModeRefreshInterval { get; set; } = 3000;
    
    public bool ShowCpu { get; set; } = true;
    public bool ShowGpu { get; set; } = true;
    public bool ShowMemory { get; set; } = true;
    public bool ShowNetwork { get; set; } = true;
    public bool ShowDisk { get; set; } = true;
    public bool ShowBattery { get; set; } = true;
    
    public int WindowX { get; set; } = 100;
    public int WindowY { get; set; } = 100;
    
    // NVIDIA 风格悬浮窗相关配置
    public bool EnableOverlay { get; set; } = true; // 是否启用悬浮窗
    public bool OverlayAlwaysOnTop { get; set; } = true; // 悬浮窗是否始终置顶
    public int OverlayFontSize { get; set; } = 16; // 悬浮窗字体大小
    public string OverlayTextColor { get; set; } = "#76B900"; // NVIDIA 绿色
    public OverlayPosition OverlayPosition { get; set; } = OverlayPosition.TopRight; // 悬浮窗位置
    public bool OverlayShowFPS { get; set; } = true; // 显示 FPS
    public bool OverlayShowGpu { get; set; } = true; // 显示 GPU
    public bool OverlayShowCpu { get; set; } = true; // 显示 CPU
    public bool OverlayShowMemory { get; set; } = true; // 显示内存
    public bool OverlayShowLatency { get; set; } = true; // 显示延迟
    
    // 启动设置
    public bool AutoStart { get; set; } = false; // 开机自启
    public bool StartMinimized { get; set; } = false; // 启动时最小化到托盘
}

public enum LayoutMode
{
    Vertical,
    Horizontal
}

public enum OverlayPosition
{
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight
}

public class SettingsService
{
    private readonly string _settingsPath;
    private Settings? _settings;

    public SettingsService()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appFolder = Path.Combine(appDataPath, "ComputerCompanion");
        Directory.CreateDirectory(appFolder);
        _settingsPath = Path.Combine(appFolder, "settings.json");
        
        LoadSettings();
    }

    public Settings GetSettings() => _settings!;

    public void SaveSettings()
    {
        var json = JsonConvert.SerializeObject(_settings, Formatting.Indented);
        File.WriteAllText(_settingsPath, json);
    }

    public void LoadSettings()
    {
        if (File.Exists(_settingsPath))
        {
            try
            {
                var json = File.ReadAllText(_settingsPath);
                _settings = JsonConvert.DeserializeObject<Settings>(json) ?? new Settings();
            }
            catch
            {
                _settings = new Settings();
            }
        }
        else
        {
            _settings = new Settings();
            SaveSettings();
        }
    }

    public void ResetToDefaults()
    {
        _settings = new Settings();
        SaveSettings();
    }
}
