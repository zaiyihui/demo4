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
}

public enum LayoutMode
{
    Vertical,
    Horizontal
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
