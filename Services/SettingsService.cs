using ComputerCompanion.Models;
using Newtonsoft.Json;
using System;
using System.IO;

namespace ComputerCompanion.Services;

public class SettingsService : ISettingsService
{
    private string _settingsPath;
    private Settings? _settings;

    public SettingsService()
    {
        _settingsPath = GetDefaultSettingsPath();
        Program.Log($"[设置] 配置文件路径: {_settingsPath}");

        try
        {
            LoadSettings();
        }
        catch (Exception ex)
        {
            Program.Log($"[设置] LoadSettings 异常: {ex.Message}");
            _settings = new Settings();
        }
    }

    private string GetDefaultSettingsPath()
    {
        try
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            if (string.IsNullOrEmpty(appDataPath))
                appDataPath = ".";

            var appFolder = Path.Combine(appDataPath, "ComputerCompanion");
            Directory.CreateDirectory(appFolder);
            return Path.Combine(appFolder, "settings.json");
        }
        catch (Exception ex)
        {
            Program.Log($"[设置] 初始化目录失败: {ex.Message}，使用当前目录");
            return Path.Combine(Directory.GetCurrentDirectory(), "settings.json");
        }
    }

    public void UpdateSettingsPath(string newPath)
    {
        if (!string.IsNullOrEmpty(newPath) && _settingsPath != newPath)
        {
            var oldPath = _settingsPath;
            _settingsPath = newPath;
            
            try
            {
                var dir = Path.GetDirectoryName(newPath);
                if (!string.IsNullOrEmpty(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                
                if (File.Exists(oldPath) && !File.Exists(newPath))
                {
                    File.Copy(oldPath, newPath);
                    Program.Log($"[设置] 配置文件已迁移到新路径: {newPath}");
                }
            }
            catch (Exception ex)
            {
                Program.Log($"[设置] 更新配置路径失败: {ex.Message}");
                _settingsPath = oldPath;
            }
        }
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
                Program.Log("[设置] 配置已保存");
            }
        }
        catch (Exception ex)
        {
            Program.Log($"[设置] 保存失败: {ex.Message}");
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
                Program.Log("[设置] 配置已加载");
            }
            catch (JsonException ex)
            {
                Program.Log($"[设置] JSON 解析失败，重建默认配置: {ex.Message}");
                try { File.Copy(_settingsPath, _settingsPath + ".bak", true); } catch { }
                _settings = new Settings();
                SaveSettings();
            }
            catch (Exception ex)
            {
                Program.Log($"[设置] 加载失败，使用默认配置: {ex.Message}");
                _settings = new Settings();
            }
        }
        else
        {
            Program.Log("[设置] 未找到配置文件，使用默认配置");
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