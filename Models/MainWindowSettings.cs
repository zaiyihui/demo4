namespace ComputerCompanion.Models;

/// <summary>
/// 主窗口布局和显示设置
/// </summary>
public class MainWindowSettings
{
    /// <summary>
    /// 布局模式（垂直/水平）
    /// </summary>
    public LayoutMode LayoutMode { get; set; } = LayoutMode.Vertical;

    /// <summary>
    /// 文字颜色
    /// </summary>
    public string TextColor { get; set; } = "#FFFFFF";

    /// <summary>
    /// 背景颜色
    /// </summary>
    public string BackgroundColor { get; set; } = "#1a1a2eea";

    /// <summary>
    /// 背景透明度（0.0 - 1.0）
    /// </summary>
    public double BackgroundOpacity { get; set; } = 0.9;

    /// <summary>
    /// 字体大小
    /// </summary>
    public int FontSize { get; set; } = 14;

    /// <summary>
    /// 窗口X坐标
    /// </summary>
    public int WindowX { get; set; } = 100;

    /// <summary>
    /// 窗口Y坐标
    /// </summary>
    public int WindowY { get; set; } = 100;
}

/// <summary>
/// 布局模式枚举
/// </summary>
public enum LayoutMode
{
    /// <summary>
    /// 垂直布局
    /// </summary>
    Vertical,

    /// <summary>
    /// 水平布局
    /// </summary>
    Horizontal
}
