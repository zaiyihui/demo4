using ComputerCompanion.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ComputerCompanion.Services;

/// <summary>
/// 颜色预设管理服务
/// 提供预设的加载、保存、导入、导出等功能
/// </summary>
public class ColorPresetService
{
    private readonly string _presetsPath;
    private ColorPresetCollection _presetCollection;

    public ColorPresetService()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appFolder = Path.Combine(appDataPath, "ComputerCompanion");
        Directory.CreateDirectory(appFolder);
        _presetsPath = Path.Combine(appFolder, "color_presets.json");

        _presetCollection = new ColorPresetCollection();
        LoadPresets();
    }

    /// <summary>
    /// 获取所有预设
    /// </summary>
    public List<ColorPreset> GetAllPresets()
    {
        return _presetCollection.Presets.ToList();
    }

    /// <summary>
    /// 获取当前预设
    /// </summary>
    public ColorPreset? GetCurrentPreset()
    {
        if (string.IsNullOrEmpty(_presetCollection.CurrentPresetId))
            return null;

        return _presetCollection.Presets.FirstOrDefault(p => p.Id == _presetCollection.CurrentPresetId);
    }

    /// <summary>
    /// 设置当前预设
    /// </summary>
    public void SetCurrentPreset(string presetId)
    {
        var preset = _presetCollection.Presets.FirstOrDefault(p => p.Id == presetId);
        if (preset != null)
        {
            _presetCollection.CurrentPresetId = presetId;
            preset.LastUsedAt = DateTime.Now;
            preset.UsageCount++;
            SavePresets();
        }
    }

    /// <summary>
    /// 获取指定类别的预设
    /// </summary>
    public List<ColorPreset> GetPresetsByCategory(ColorPresetCategory category)
    {
        return _presetCollection.Presets
            .Where(p => p.Category == category)
            .OrderByDescending(p => p.UsageCount)
            .ToList();
    }

    /// <summary>
    /// 添加自定义预设
    /// </summary>
    public ColorPreset AddPreset(ColorPreset preset)
    {
        preset.Id = Guid.NewGuid().ToString();
        preset.Category = ColorPresetCategory.Custom;
        preset.IsSystemPreset = false;
        preset.CreatedAt = DateTime.Now;
        preset.LastUsedAt = DateTime.Now;
        preset.UsageCount = 0;

        _presetCollection.Presets.Add(preset);
        SavePresets();

        return preset;
    }

    /// <summary>
    /// 更新预设
    /// </summary>
    public void UpdatePreset(ColorPreset preset)
    {
        var existing = _presetCollection.Presets.FirstOrDefault(p => p.Id == preset.Id);
        if (existing != null && !existing.IsSystemPreset)
        {
            existing.Name = preset.Name;
            existing.Description = preset.Description;
            existing.TextColor = preset.TextColor;
            existing.BackgroundColor = preset.BackgroundColor;
            existing.BackgroundOpacity = preset.BackgroundOpacity;

            SavePresets();
        }
    }

    /// <summary>
    /// 删除预设（仅自定义预设可删除）
    /// </summary>
    public bool DeletePreset(string presetId)
    {
        var preset = _presetCollection.Presets.FirstOrDefault(p => p.Id == presetId);
        if (preset != null && !preset.IsSystemPreset)
        {
            _presetCollection.Presets.Remove(preset);

            if (_presetCollection.CurrentPresetId == presetId)
            {
                _presetCollection.CurrentPresetId = null;
            }

            SavePresets();
            return true;
        }
        return false;
    }

    /// <summary>
    /// 重置为系统默认预设
    /// </summary>
    public void ResetToDefaults()
    {
        _presetCollection.Presets.Clear();
        _presetCollection.Presets.AddRange(DefaultColorPresets.GetAllSystemPresets());
        _presetCollection.CurrentPresetId = "nvidia-green";
        SavePresets();
    }

    /// <summary>
    /// 导出预设到文件
    /// </summary>
    public bool ExportPresets(string filePath)
    {
        try
        {
            var customPresets = _presetCollection.Presets
                .Where(p => !p.IsSystemPreset)
                .ToList();

            if (customPresets.Count == 0)
            {
                Console.WriteLine("没有可导出的自定义预设");
                return false;
            }

            var exportData = new ColorPresetCollection
            {
                Presets = customPresets,
                LastUpdated = DateTime.Now
            };

            var json = JsonConvert.SerializeObject(exportData, Formatting.Indented);
            File.WriteAllText(filePath, json);

            Console.WriteLine($"成功导出 {customPresets.Count} 个预设到: {filePath}");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"导出预设失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 从文件导入预设
    /// </summary>
    public int ImportPresets(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"文件不存在: {filePath}");
                return 0;
            }

            var json = File.ReadAllText(filePath);
            var importedCollection = ColorPresetCollection.FromJson(json);

            if (importedCollection.Presets.Count == 0)
            {
                Console.WriteLine("导入的文件中没有预设");
                return 0;
            }

            int importedCount = 0;
            foreach (var preset in importedCollection.Presets)
            {
                // 检查是否已存在（根据名称匹配）
                var existing = _presetCollection.Presets
                    .FirstOrDefault(p => p.Name == preset.Name && !p.IsSystemPreset);

                if (existing != null)
                {
                    // 更新现有预设
                    existing.TextColor = preset.TextColor;
                    existing.BackgroundColor = preset.BackgroundColor;
                    existing.BackgroundOpacity = preset.BackgroundOpacity;
                    existing.Description = preset.Description;
                }
                else
                {
                    // 添加新预设
                    preset.Id = Guid.NewGuid().ToString();
                    preset.IsSystemPreset = false;
                    preset.CreatedAt = DateTime.Now;
                    preset.UsageCount = 0;
                    _presetCollection.Presets.Add(preset);
                }

                importedCount++;
            }

            SavePresets();
            Console.WriteLine($"成功导入 {importedCount} 个预设");
            return importedCount;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"导入预设失败: {ex.Message}");
            return 0;
        }
    }

    /// <summary>
    /// 获取最近使用的预设
    /// </summary>
    public List<ColorPreset> GetRecentlyUsedPresets(int count = 5)
    {
        return _presetCollection.Presets
            .Where(p => p.UsageCount > 0)
            .OrderByDescending(p => p.LastUsedAt)
            .Take(count)
            .ToList();
    }

    /// <summary>
    /// 搜索预设
    /// </summary>
    public List<ColorPreset> SearchPresets(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return GetAllPresets();

        keyword = keyword.ToLower();
        return _presetCollection.Presets
            .Where(p => p.Name.ToLower().Contains(keyword) ||
                       p.Description.ToLower().Contains(keyword))
            .OrderByDescending(p => p.UsageCount)
            .ToList();
    }

    private void LoadPresets()
    {
        try
        {
            if (File.Exists(_presetsPath))
            {
                var json = File.ReadAllText(_presetsPath);
                _presetCollection = ColorPresetCollection.FromJson(json);

                // 确保系统预设存在
                EnsureSystemPresets();
            }
            else
            {
                // 首次使用，加载默认预设
                ResetToDefaults();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"加载颜色预设失败: {ex.Message}");
            ResetToDefaults();
        }
    }

    private void EnsureSystemPresets()
    {
        var systemPresets = DefaultColorPresets.GetAllSystemPresets();

        foreach (var systemPreset in systemPresets)
        {
            var existing = _presetCollection.Presets.FirstOrDefault(p => p.Id == systemPreset.Id);
            if (existing == null)
            {
                _presetCollection.Presets.Add(systemPreset);
            }
        }
    }

    private void SavePresets()
    {
        try
        {
            _presetCollection.LastUpdated = DateTime.Now;
            var json = JsonConvert.SerializeObject(_presetCollection, Formatting.Indented);
            File.WriteAllText(_presetsPath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"保存颜色预设失败: {ex.Message}");
        }
    }
}
