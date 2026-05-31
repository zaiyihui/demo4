namespace ComputerCompanion.Models;

/// <summary>
/// 应用程序设置
/// 组合所有配置子模块，提供统一的配置管理接口
/// </summary>
public class Settings
{
    #region 子模块配置

    /// <summary>
    /// 主窗口布局和显示设置
    /// </summary>
    public MainWindowSettings MainWindow { get; set; } = new MainWindowSettings();

    /// <summary>
    /// 悬浮窗显示设置
    /// </summary>
    public OverlaySettings Overlay { get; set; } = new OverlaySettings();

    /// <summary>
    /// 主窗口显示内容设置
    /// </summary>
    public DisplayContentSettings DisplayContent { get; set; } = new DisplayContentSettings();

    /// <summary>
    /// 性能监控设置
    /// </summary>
    public PerformanceSettings Performance { get; set; } = new PerformanceSettings();

    /// <summary>
    /// 启动设置
    /// </summary>
    public StartupSettings Startup { get; set; } = new StartupSettings();

    #endregion

    #region 向后兼容属性（已废弃，建议使用子模块）

    /// <summary>
    /// 布局模式
    /// </summary>
    [System.Obsolete("请使用 MainWindow.LayoutMode")]
    public LayoutMode LayoutMode
    {
        get => MainWindow.LayoutMode;
        set => MainWindow.LayoutMode = value;
    }

    /// <summary>
    /// 文字颜色
    /// </summary>
    [System.Obsolete("请使用 MainWindow.TextColor")]
    public string TextColor
    {
        get => MainWindow.TextColor;
        set => MainWindow.TextColor = value;
    }

    /// <summary>
    /// 背景颜色
    /// </summary>
    [System.Obsolete("请使用 MainWindow.BackgroundColor")]
    public string BackgroundColor
    {
        get => MainWindow.BackgroundColor;
        set => MainWindow.BackgroundColor = value;
    }

    /// <summary>
    /// 背景透明度
    /// </summary>
    [System.Obsolete("请使用 MainWindow.BackgroundOpacity")]
    public double BackgroundOpacity
    {
        get => MainWindow.BackgroundOpacity;
        set => MainWindow.BackgroundOpacity = value;
    }

    /// <summary>
    /// 字体大小
    /// </summary>
    [System.Obsolete("请使用 MainWindow.FontSize")]
    public int FontSize
    {
        get => MainWindow.FontSize;
        set => MainWindow.FontSize = value;
    }

    /// <summary>
    /// 窗口X坐标
    /// </summary>
    [System.Obsolete("请使用 MainWindow.WindowX")]
    public int WindowX
    {
        get => MainWindow.WindowX;
        set => MainWindow.WindowX = value;
    }

    /// <summary>
    /// 窗口Y坐标
    /// </summary>
    [System.Obsolete("请使用 MainWindow.WindowY")]
    public int WindowY
    {
        get => MainWindow.WindowY;
        set => MainWindow.WindowY = value;
    }

    /// <summary>
    /// 是否启用悬浮窗
    /// </summary>
    [System.Obsolete("请使用 Overlay.EnableOverlay")]
    public bool EnableOverlay
    {
        get => Overlay.EnableOverlay;
        set => Overlay.EnableOverlay = value;
    }

    /// <summary>
    /// 是否窗口置顶
    /// </summary>
    [System.Obsolete("请使用 Overlay.OverlayAlwaysOnTop")]
    public bool OverlayAlwaysOnTop
    {
        get => Overlay.OverlayAlwaysOnTop;
        set => Overlay.OverlayAlwaysOnTop = value;
    }

    /// <summary>
    /// 悬浮窗字体大小
    /// </summary>
    [System.Obsolete("请使用 Overlay.OverlayFontSize")]
    public int OverlayFontSize
    {
        get => Overlay.OverlayFontSize;
        set => Overlay.OverlayFontSize = value;
    }

    /// <summary>
    /// 悬浮窗文字颜色
    /// </summary>
    [System.Obsolete("请使用 Overlay.OverlayTextColor")]
    public string OverlayTextColor
    {
        get => Overlay.OverlayTextColor;
        set => Overlay.OverlayTextColor = value;
    }

    /// <summary>
    /// 悬浮窗背景颜色
    /// </summary>
    [System.Obsolete("请使用 Overlay.OverlayBackgroundColor")]
    public string OverlayBackgroundColor
    {
        get => Overlay.OverlayBackgroundColor;
        set => Overlay.OverlayBackgroundColor = value;
    }

    /// <summary>
    /// 悬浮窗位置
    /// </summary>
    [System.Obsolete("请使用 Overlay.OverlayPosition")]
    public OverlayPosition OverlayPosition
    {
        get => Overlay.OverlayPosition;
        set => Overlay.OverlayPosition = value;
    }

    /// <summary>
    /// 是否显示FPS
    /// </summary>
    [System.Obsolete("请使用 Overlay.OverlayShowFPS")]
    public bool OverlayShowFPS
    {
        get => Overlay.OverlayShowFPS;
        set => Overlay.OverlayShowFPS = value;
    }

    /// <summary>
    /// 是否显示GPU
    /// </summary>
    [System.Obsolete("请使用 Overlay.OverlayShowGpu")]
    public bool OverlayShowGpu
    {
        get => Overlay.OverlayShowGpu;
        set => Overlay.OverlayShowGpu = value;
    }

    /// <summary>
    /// 是否显示CPU
    /// </summary>
    [System.Obsolete("请使用 Overlay.OverlayShowCpu")]
    public bool OverlayShowCpu
    {
        get => Overlay.OverlayShowCpu;
        set => Overlay.OverlayShowCpu = value;
    }

    /// <summary>
    /// 是否显示内存
    /// </summary>
    [System.Obsolete("请使用 Overlay.OverlayShowMemory")]
    public bool OverlayShowMemory
    {
        get => Overlay.OverlayShowMemory;
        set => Overlay.OverlayShowMemory = value;
    }

    /// <summary>
    /// 是否显示延迟
    /// </summary>
    [System.Obsolete("请使用 Overlay.OverlayShowLatency")]
    public bool OverlayShowLatency
    {
        get => Overlay.OverlayShowLatency;
        set => Overlay.OverlayShowLatency = value;
    }

    /// <summary>
    /// 是否显示CPU（主窗口）
    /// </summary>
    [System.Obsolete("请使用 DisplayContent.ShowCpu")]
    public bool ShowCpu
    {
        get => DisplayContent.ShowCpu;
        set => DisplayContent.ShowCpu = value;
    }

    /// <summary>
    /// 是否显示GPU（主窗口）
    /// </summary>
    [System.Obsolete("请使用 DisplayContent.ShowGpu")]
    public bool ShowGpu
    {
        get => DisplayContent.ShowGpu;
        set => DisplayContent.ShowGpu = value;
    }

    /// <summary>
    /// 是否显示内存（主窗口）
    /// </summary>
    [System.Obsolete("请使用 DisplayContent.ShowMemory")]
    public bool ShowMemory
    {
        get => DisplayContent.ShowMemory;
        set => DisplayContent.ShowMemory = value;
    }

    /// <summary>
    /// 是否显示网络（主窗口）
    /// </summary>
    [System.Obsolete("请使用 DisplayContent.ShowNetwork")]
    public bool ShowNetwork
    {
        get => DisplayContent.ShowNetwork;
        set => DisplayContent.ShowNetwork = value;
    }

    /// <summary>
    /// 是否显示磁盘（主窗口）
    /// </summary>
    [System.Obsolete("请使用 DisplayContent.ShowDisk")]
    public bool ShowDisk
    {
        get => DisplayContent.ShowDisk;
        set => DisplayContent.ShowDisk = value;
    }

    /// <summary>
    /// 是否显示电池（主窗口）
    /// </summary>
    [System.Obsolete("请使用 DisplayContent.ShowBattery")]
    public bool ShowBattery
    {
        get => DisplayContent.ShowBattery;
        set => DisplayContent.ShowBattery = value;
    }

    /// <summary>
    /// 刷新间隔
    /// </summary>
    [System.Obsolete("请使用 Performance.RefreshInterval")]
    public int RefreshInterval
    {
        get => Performance.RefreshInterval;
        set => Performance.RefreshInterval = value;
    }

    /// <summary>
    /// 是否启用游戏模式
    /// </summary>
    [System.Obsolete("请使用 Performance.GameMode")]
    public bool GameMode
    {
        get => Performance.GameMode;
        set => Performance.GameMode = value;
    }

    /// <summary>
    /// 游戏模式刷新间隔
    /// </summary>
    [System.Obsolete("请使用 Performance.GameModeRefreshInterval")]
    public int GameModeRefreshInterval
    {
        get => Performance.GameModeRefreshInterval;
        set => Performance.GameModeRefreshInterval = value;
    }

    /// <summary>
    /// 开机自动启动
    /// </summary>
    [System.Obsolete("请使用 Startup.AutoStart")]
    public bool AutoStart
    {
        get => Startup.AutoStart;
        set => Startup.AutoStart = value;
    }

    /// <summary>
    /// 启动时最小化
    /// </summary>
    [System.Obsolete("请使用 Startup.StartMinimized")]
    public bool StartMinimized
    {
        get => Startup.StartMinimized;
        set => Startup.StartMinimized = value;
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 重置为默认设置
    /// </summary>
    public void ResetToDefaults()
    {
        MainWindow = new MainWindowSettings();
        Overlay = new OverlaySettings();
        DisplayContent = new DisplayContentSettings();
        Performance = new PerformanceSettings();
        Startup = new StartupSettings();
    }

    /// <summary>
    /// 从旧格式迁移
    /// </summary>
    public void MigrateFromLegacy()
    {
        // 此方法用于从旧的扁平结构迁移数据
        // 如果需要，可以在这里添加迁移逻辑
    }

    #endregion
}
