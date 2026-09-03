namespace ComputerCompanion.Models;

/// <summary>
/// 应用程序启动设置
/// </summary>
public class StartupSettings
{
    /// <summary>
    /// 是否开机自动启动
    /// </summary>
    public bool AutoStart { get; set; } = false;

    /// <summary>
    /// 启动时是否最小化
    /// </summary>
    public bool StartMinimized { get; set; } = false;

    /// <summary>
    /// 是否已显示过管理员权限引导
    /// </summary>
    public bool HasShownAdminPrompt { get; set; } = false;
}
