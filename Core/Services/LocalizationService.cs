using ComputerCompanion.Core.Abstractions;
using ComputerCompanion.Core.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ComputerCompanion.Core.Services;

/// <summary>
/// 本地化服务 - 实现多语言支持、文化切换、RTL支持
/// </summary>
public class LocalizationService : ServiceBase, ILocalizationService
{
    private readonly Dictionary<string, Dictionary<string, string>> _translations = new();
    private string _currentCulture = "zh-CN";
    private readonly object _lock = new object();

    public string CurrentCulture => _currentCulture;

    public IReadOnlyList<CultureInfoModel> SupportedCultures { get; } = new List<CultureInfoModel>
    {
        new CultureInfoModel
        {
            Code = "zh-CN",
            Name = "简体中文",
            NativeName = "简体中文",
            IsRTL = false,
            FlagEmoji = "🇨🇳"
        },
        new CultureInfoModel
        {
            Code = "zh-TW",
            Name = "繁体中文",
            NativeName = "繁體中文",
            IsRTL = false,
            FlagEmoji = "🇹🇼"
        },
        new CultureInfoModel
        {
            Code = "en-US",
            Name = "English",
            NativeName = "English",
            IsRTL = false,
            FlagEmoji = "🇺🇸"
        },
        new CultureInfoModel
        {
            Code = "ja-JP",
            Name = "Japanese",
            NativeName = "日本語",
            IsRTL = false,
            FlagEmoji = "🇯🇵"
        },
        new CultureInfoModel
        {
            Code = "ko-KR",
            Name = "Korean",
            NativeName = "한국어",
            IsRTL = false,
            FlagEmoji = "🇰🇷"
        },
        new CultureInfoModel
        {
            Code = "de-DE",
            Name = "German",
            NativeName = "Deutsch",
            IsRTL = false,
            FlagEmoji = "🇩🇪"
        },
        new CultureInfoModel
        {
            Code = "fr-FR",
            Name = "French",
            NativeName = "Français",
            IsRTL = false,
            FlagEmoji = "🇫🇷"
        },
        new CultureInfoModel
        {
            Code = "es-ES",
            Name = "Spanish",
            NativeName = "Español",
            IsRTL = false,
            FlagEmoji = "🇪🇸"
        },
        new CultureInfoModel
        {
            Code = "ar-SA",
            Name = "Arabic",
            NativeName = "العربية",
            IsRTL = true,
            FlagEmoji = "🇸🇦"
        }
    };

    public event EventHandler<CultureChangedEventArgs>? CultureChanged;

    public LocalizationService()
    {
        InitializeTranslations();
    }

    private void InitializeTranslations()
    {
        // 简体中文翻译
        _translations["zh-CN"] = new Dictionary<string, string>
        {
            // 主窗口
            ["App.Title"] = "电脑伴侣",
            ["App.Subtitle"] = "硬件监控工具",
            ["Menu.File"] = "文件",
            ["Menu.Settings"] = "设置",
            ["Menu.Help"] = "帮助",
            ["Menu.Exit"] = "退出",
            ["Menu.About"] = "关于",

            // 监控项
            ["Monitor.CPU"] = "处理器",
            ["Monitor.GPU"] = "显卡",
            ["Monitor.Memory"] = "内存",
            ["Monitor.Disk"] = "磁盘",
            ["Monitor.Network"] = "网络",
            ["Monitor.Battery"] = "电池",
            ["Monitor.FPS"] = "帧率",

            // 通用
            ["Common.Usage"] = "使用率",
            ["Common.Temperature"] = "温度",
            ["Common.Speed"] = "速度",
            ["Common.Status"] = "状态",
            ["Common.OK"] = "确定",
            ["Common.Cancel"] = "取消",
            ["Common.Save"] = "保存",
            ["Common.Close"] = "关闭",
            ["Common.Enable"] = "启用",
            ["Common.Disable"] = "禁用",
            ["Common.Refresh"] = "刷新",

            // 状态
            ["Status.Running"] = "运行中",
            ["Status.Stopped"] = "已停止",
            ["Status.Error"] = "错误",
            ["Status.Normal"] = "正常",
            ["Status.Warning"] = "警告",
            ["Status.Critical"] = "严重",

            // 设置
            ["Settings.Title"] = "设置",
            ["Settings.General"] = "常规",
            ["Settings.Appearance"] = "外观",
            ["Settings.Performance"] = "性能",
            ["Settings.Backup"] = "备份",
            ["Settings.Language"] = "语言",
            ["Settings.Theme"] = "主题",
            ["Settings.Startup"] = "启动",
            ["Settings.AutoStart"] = "开机启动",
            ["Settings.StartMinimized"] = "启动时最小化",

            // 备份
            ["Backup.Title"] = "配置备份",
            ["Backup.Create"] = "创建备份",
            ["Backup.Restore"] = "恢复备份",
            ["Backup.Delete"] = "删除备份",
            ["Backup.Full"] = "完整备份",
            ["Backup.Differential"] = "差异备份",
            ["Backup.Auto"] = "自动备份",
            ["Backup.Interval"] = "备份间隔",
            ["Backup.LastBackup"] = "上次备份",
            ["Backup.NoBackups"] = "暂无备份",

            // 错误消息
            ["Error.BackupFailed"] = "备份失败",
            ["Error.RestoreFailed"] = "恢复失败",
            ["Error.LoadSettingsFailed"] = "加载设置失败",
            ["Error.SaveSettingsFailed"] = "保存设置失败",

            // 成功消息
            ["Success.BackupCreated"] = "备份创建成功",
            ["Success.RestoreCompleted"] = "恢复完成",
            ["Success.SettingsSaved"] = "设置已保存"
        };

        // 英语翻译
        _translations["en-US"] = new Dictionary<string, string>
        {
            ["App.Title"] = "Computer Companion",
            ["App.Subtitle"] = "Hardware Monitor",
            ["Menu.File"] = "File",
            ["Menu.Settings"] = "Settings",
            ["Menu.Help"] = "Help",
            ["Menu.Exit"] = "Exit",
            ["Menu.About"] = "About",

            ["Monitor.CPU"] = "CPU",
            ["Monitor.GPU"] = "GPU",
            ["Monitor.Memory"] = "Memory",
            ["Monitor.Disk"] = "Disk",
            ["Monitor.Network"] = "Network",
            ["Monitor.Battery"] = "Battery",
            ["Monitor.FPS"] = "FPS",

            ["Common.Usage"] = "Usage",
            ["Common.Temperature"] = "Temperature",
            ["Common.Speed"] = "Speed",
            ["Common.Status"] = "Status",
            ["Common.OK"] = "OK",
            ["Common.Cancel"] = "Cancel",
            ["Common.Save"] = "Save",
            ["Common.Close"] = "Close",
            ["Common.Enable"] = "Enable",
            ["Common.Disable"] = "Disable",
            ["Common.Refresh"] = "Refresh",

            ["Status.Running"] = "Running",
            ["Status.Stopped"] = "Stopped",
            ["Status.Error"] = "Error",
            ["Status.Normal"] = "Normal",
            ["Status.Warning"] = "Warning",
            ["Status.Critical"] = "Critical",

            ["Settings.Title"] = "Settings",
            ["Settings.General"] = "General",
            ["Settings.Appearance"] = "Appearance",
            ["Settings.Performance"] = "Performance",
            ["Settings.Backup"] = "Backup",
            ["Settings.Language"] = "Language",
            ["Settings.Theme"] = "Theme",
            ["Settings.Startup"] = "Startup",
            ["Settings.AutoStart"] = "Auto Start",
            ["Settings.StartMinimized"] = "Start Minimized",

            ["Backup.Title"] = "Configuration Backup",
            ["Backup.Create"] = "Create Backup",
            ["Backup.Restore"] = "Restore Backup",
            ["Backup.Delete"] = "Delete Backup",
            ["Backup.Full"] = "Full Backup",
            ["Backup.Differential"] = "Differential Backup",
            ["Backup.Auto"] = "Auto Backup",
            ["Backup.Interval"] = "Backup Interval",
            ["Backup.LastBackup"] = "Last Backup",
            ["Backup.NoBackups"] = "No backups available",

            ["Error.BackupFailed"] = "Backup failed",
            ["Error.RestoreFailed"] = "Restore failed",
            ["Error.LoadSettingsFailed"] = "Failed to load settings",
            ["Error.SaveSettingsFailed"] = "Failed to save settings",

            ["Success.BackupCreated"] = "Backup created successfully",
            ["Success.RestoreCompleted"] = "Restore completed",
            ["Success.SettingsSaved"] = "Settings saved"
        };

        // 如果当前文化不在列表中，默认为简体中文
        if (!_translations.ContainsKey(_currentCulture))
        {
            _currentCulture = "zh-CN";
        }
    }

    public override Task InitializeAsync()
    {
        base.InitializeAsync();

        // 设置当前文化的线程文化
        try
        {
            var culture = new CultureInfo(_currentCulture);
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
        }
        catch { }

        Program.Log($"[本地化] 已初始化，当前文化: {_currentCulture}");
        return Task.CompletedTask;
    }

    /// <summary>
    /// 获取本地化字符串
    /// </summary>
    public string GetString(string key, params object[] args)
    {
        lock (_lock)
        {
            // 尝试获取当前语言的翻译
            if (_translations.TryGetValue(_currentCulture, out var dict))
            {
                if (dict.TryGetValue(key, out var value))
                {
                    return args.Length > 0 ? string.Format(value, args) : value;
                }
            }

            // 回退到简体中文
            if (_currentCulture != "zh-CN" && _translations.TryGetValue("zh-CN", out var zhDict))
            {
                if (zhDict.TryGetValue(key, out var value))
                {
                    return args.Length > 0 ? string.Format(value, args) : value;
                }
            }

            // 返回原始键
            return key;
        }
    }

    /// <summary>
    /// 设置文化
    /// </summary>
    public void SetCulture(string cultureCode)
    {
        if (string.IsNullOrEmpty(cultureCode) || !_translations.ContainsKey(cultureCode))
        {
            Program.Log($"[本地化] 不支持的文化代码: {cultureCode}");
            return;
        }

        var oldCulture = _currentCulture;
        _currentCulture = cultureCode;

        try
        {
            var culture = new CultureInfo(cultureCode);
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
        }
        catch (Exception ex)
        {
            Program.Log($"[本地化] 设置线程文化失败: {ex.Message}");
        }

        CultureChanged?.Invoke(this, new CultureChangedEventArgs
        {
            OldCulture = oldCulture,
            NewCulture = _currentCulture
        });

        Program.Log($"[本地化] 文化已切换: {oldCulture} -> {_currentCulture}");
    }

    /// <summary>
    /// 获取指定文化的翻译（用于预览）
    /// </summary>
    public string GetStringForCulture(string cultureCode, string key)
    {
        if (_translations.TryGetValue(cultureCode, out var dict))
        {
            if (dict.TryGetValue(key, out var value))
            {
                return value;
            }
        }
        return key;
    }

    /// <summary>
    /// 添加自定义翻译
    /// </summary>
    public void AddTranslation(string cultureCode, string key, string value)
    {
        lock (_lock)
        {
            if (!_translations.ContainsKey(cultureCode))
            {
                _translations[cultureCode] = new Dictionary<string, string>();
            }
            _translations[cultureCode][key] = value;
        }
    }

    /// <summary>
    /// 导出翻译到文件
    /// </summary>
    public async Task ExportTranslationsAsync(string filePath)
    {
        var json = JsonConvert.SerializeObject(_translations, Formatting.Indented);
        await File.WriteAllTextAsync(filePath, json);
        Program.Log($"[本地化] 翻译已导出到: {filePath}");
    }

    /// <summary>
    /// 从文件导入翻译
    /// </summary>
    public async Task ImportTranslationsAsync(string filePath)
    {
        var json = await File.ReadAllTextAsync(filePath);
        var imported = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(json);

        if (imported != null)
        {
            foreach (var culture in imported)
            {
                if (!_translations.ContainsKey(culture.Key))
                {
                    _translations[culture.Key] = new Dictionary<string, string>();
                }

                foreach (var translation in culture.Value)
                {
                    _translations[culture.Key][translation.Key] = translation.Value;
                }
            }

            Program.Log($"[本地化] 已从 {filePath} 导入翻译");
        }
    }

    /// <summary>
    /// 检查文化是否支持RTL
    /// </summary>
    public bool IsRTL(string cultureCode)
    {
        var culture = SupportedCultures.FirstOrDefault(c => c.Code == cultureCode);
        return culture?.IsRTL ?? false;
    }

    /// <summary>
    /// 获取格式化字符串
    /// </summary>
    public string GetFormattedString(string key, CultureInfo culture)
    {
        var value = GetString(key);
        return string.Format(culture, value);
    }
}
