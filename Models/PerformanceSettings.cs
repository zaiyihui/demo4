using ComputerCompanion.Services;

namespace ComputerCompanion.Models;

/// <summary>
/// 性能监控设置
/// </summary>
public class PerformanceSettings
{
    /// <summary>
    /// 刷新间隔（毫秒）
    /// </summary>
    public int RefreshInterval { get; set; } = 1000;

    /// <summary>
    /// 是否启用游戏模式
    /// </summary>
    public bool GameMode { get; set; } = false;

    /// <summary>
    /// 游戏模式刷新间隔（毫秒）
    /// </summary>
    public int GameModeRefreshInterval { get; set; } = 3000;

    /// <summary>
    /// 主题模式
    /// </summary>
    public ThemeMode ThemeMode { get; set; } = ThemeMode.Dark;

    /// <summary>
    /// 是否启用自动备份
    /// </summary>
    public bool AutoBackupEnabled { get; set; } = true;

    /// <summary>
    /// 自动备份间隔（小时）
    /// </summary>
    public int AutoBackupIntervalHours { get; set; } = 24;

    /// <summary>
    /// 是否启用差异备份
    /// </summary>
    public bool DifferentialBackupEnabled { get; set; } = false;
}
