using System;
using System.Collections.Generic;
using System.IO;
using ComputerCompanion.Models;
using ComputerCompanion.ViewModels;
using Newtonsoft.Json;

namespace ComputerCompanion.Services;

/// <summary>
/// 皮肤/布局模板服务：加载内置预设与自定义预设，并应用到 ViewModel
/// </summary>
public static class SkinService
{
    private const string CustomSkinsDirectory = "Assets/Skins";

    private static readonly Dictionary<string, SkinPreset> _cache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 获取所有可用皮肤（内置 + 自定义）
    /// </summary>
    public static IReadOnlyList<SkinPreset> GetAllSkins()
    {
        var list = new List<SkinPreset>(SkinPreset.BuiltInPresets);
        list.AddRange(LoadCustomSkins());
        return list;
    }

    /// <summary>
    /// 根据名称获取内置皮肤预设
    /// </summary>
    public static SkinPreset? GetBuiltInSkin(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        foreach (var preset in SkinPreset.BuiltInPresets)
        {
            if (string.Equals(preset.Name, name, StringComparison.OrdinalIgnoreCase))
                return preset;
        }

        return null;
    }

    /// <summary>
    /// 根据名称获取皮肤预设（内置 + 自定义）
    /// </summary>
    public static SkinPreset? GetSkin(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        if (_cache.TryGetValue(name, out var cached))
            return cached;

        var builtIn = GetBuiltInSkin(name);
        if (builtIn != null)
        {
            _cache[builtIn.Name] = builtIn;
            return builtIn;
        }

        foreach (var custom in LoadCustomSkins())
        {
            if (string.Equals(custom.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                _cache[custom.Name] = custom;
                return custom;
            }
        }

        return null;
    }

    /// <summary>
    /// 将皮肤预设应用到 OverlayViewModel
    /// </summary>
    public static void ApplySkin(OverlayViewModel viewModel, SkinPreset skin)
    {
        if (viewModel == null || skin == null)
            return;

        viewModel.OverlayTextColor = skin.TextColor;
    }

    /// <summary>
    /// 从 Assets/Skins/*.json 加载自定义皮肤预设
    /// </summary>
    private static List<SkinPreset> LoadCustomSkins()
    {
        var result = new List<SkinPreset>();

        var directory = Path.Combine(AppContext.BaseDirectory, CustomSkinsDirectory);
        if (!Directory.Exists(directory))
            return result;

        foreach (var file in Directory.EnumerateFiles(directory, "*.json"))
        {
            try
            {
                var json = File.ReadAllText(file);
                var preset = JsonConvert.DeserializeObject<SkinPreset>(json);
                if (preset != null && !string.IsNullOrWhiteSpace(preset.Name))
                {
                    result.Add(preset);
                }
            }
            catch
            {
                // 忽略损坏的皮肤文件
            }
        }

        return result;
    }
}
