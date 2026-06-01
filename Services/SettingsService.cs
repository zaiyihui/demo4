using ComputerCompanion.Models;
using Newtonsoft.Json;
using System;
using System.IO;

namespace ComputerCompanion.Services;

public class SettingsService : ISettingsService
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

    public Settings GetSettings() => _settings ?? new Settings();

    public void SaveSettings()
    {
        try
        {
            if (_settings != null)
            {
                var json = JsonConvert.SerializeObject(_settings, Formatting.Indented);
                File.WriteAllText(_settingsPath, json);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"保存设置失败: {ex.Message}");
        }
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
            catch (Exception ex)
            {
                Console.WriteLine($"加载设置失败: {ex.Message}");
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