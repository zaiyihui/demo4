using System;
using System.Collections.Generic;

namespace ComputerCompanion.Models;

/// <summary>
/// 颜色预设方案
/// </summary>
public class ColorPreset
{
    /// <summary>
    /// 预设唯一标识符
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 预设名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 预设描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 预设类别
    /// </summary>
    public ColorPresetCategory Category { get; set; } = ColorPresetCategory.Custom;

    /// <summary>
    /// 文字颜色（十六进制格式，如 "#76B900"）
    /// </summary>
    public string TextColor { get; set; } = "#FFFFFF";

    /// <summary>
    /// 背景颜色（十六进制格式）
    /// </summary>
    public string BackgroundColor { get; set; } = "#1a1a2eea";

    /// <summary>
    /// 背景透明度（0.0 - 1.0）
    /// </summary>
    public double BackgroundOpacity { get; set; } = 0.9;

    /// <summary>
    /// 是否为系统预设（系统预设不可删除）
    /// </summary>
    public bool IsSystemPreset { get; set; } = false;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// 最后使用时间
    /// </summary>
    public DateTime LastUsedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// 使用次数
    /// </summary>
    public int UsageCount { get; set; } = 0;
}

/// <summary>
/// 预设类别
/// </summary>
public enum ColorPresetCategory
{
    /// <summary>
    /// 专业级预设
    /// </summary>
    Professional,

    /// <summary>
    /// 游戏级预设
    /// </summary>
    Gaming,

    /// <summary>
    /// 简洁风格预设
    /// </summary>
    Minimal,

    /// <summary>
    /// 自定义预设
    /// </summary>
    Custom
}

/// <summary>
/// 颜色预设集合
/// </summary>
public class ColorPresetCollection
{
    /// <summary>
    /// 预设列表
    /// </summary>
    public List<ColorPreset> Presets { get; set; } = new List<ColorPreset>();

    /// <summary>
    /// 当前使用的预设ID
    /// </summary>
    public string? CurrentPresetId { get; set; }

    /// <summary>
    /// 最后更新时间
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.Now;

    /// <summary>
    /// 导出为 JSON 格式
    /// </summary>
    public string ToJson()
    {
        return Newtonsoft.Json.JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented);
    }

    /// <summary>
    /// 从 JSON 导入
    /// </summary>
    public static ColorPresetCollection FromJson(string json)
    {
        try
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<ColorPresetCollection>(json) ?? new ColorPresetCollection();
        }
        catch
        {
            return new ColorPresetCollection();
        }
    }
}

/// <summary>
/// 预定义的颜色预设方案
/// </summary>
public static class DefaultColorPresets
{
    /// <summary>
    /// 获取所有系统预设
    /// </summary>
    public static List<ColorPreset> GetAllSystemPresets()
    {
        return new List<ColorPreset>
        {
            // 专业级预设 - NVIDIA 风格
            new ColorPreset
            {
                Id = "nvidia-green",
                Name = "NVIDIA 绿",
                Description = "经典的 NVIDIA 风格，绿色文字配深蓝背景",
                Category = ColorPresetCategory.Professional,
                TextColor = "#76B900",
                BackgroundColor = "#1a1a2eea",
                BackgroundOpacity = 0.9,
                IsSystemPreset = true
            },

            // 专业级预设 - AMD 风格
            new ColorPreset
            {
                Id = "amd-red",
                Name = "AMD 红",
                Description = "AMD 风格的红色主题",
                Category = ColorPresetCategory.Professional,
                TextColor = "#D02F2F",
                BackgroundColor = "#1a1a1aea",
                BackgroundOpacity = 0.85,
                IsSystemPreset = true
            },

            // 游戏级预设 - 赛博朋克
            new ColorPreset
            {
                Id = "cyberpunk",
                Name = "赛博朋克",
                Description = "未来科技感配色，青色和品红色调",
                Category = ColorPresetCategory.Gaming,
                TextColor = "#00FFFF",
                BackgroundColor = "#FF00FF",
                BackgroundOpacity = 0.3,
                IsSystemPreset = true
            },

            // 游戏级预设 - 游戏暗夜
            new ColorPreset
            {
                Id = "gaming-dark",
                Name = "游戏暗夜",
                Description = "深色背景配亮白文字，适合夜间游戏",
                Category = ColorPresetCategory.Gaming,
                TextColor = "#FFFFFF",
                BackgroundColor = "#000000",
                BackgroundOpacity = 0.7,
                IsSystemPreset = true
            },

            // 简洁风格预设 - 透明极简
            new ColorPreset
            {
                Id = "minimal-transparent",
                Name = "透明极简",
                Description = "透明背景配白色文字，极简风格",
                Category = ColorPresetCategory.Minimal,
                TextColor = "#FFFFFF",
                BackgroundColor = "#000000",
                BackgroundOpacity = 0.1,
                IsSystemPreset = true
            },

            // 简洁风格预设 - 温暖米色
            new ColorPreset
            {
                Id = "warm-beige",
                Name = "温暖米色",
                Description = "温暖的米色配色，舒适护眼",
                Category = ColorPresetCategory.Minimal,
                TextColor = "#333333",
                BackgroundColor = "#F5F5DC",
                BackgroundOpacity = 0.95,
                IsSystemPreset = true
            }
        };
    }
}
