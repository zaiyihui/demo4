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
}
