namespace ComputerCompanion.Models;

/// <summary>
/// 悬浮窗显示设置
/// </summary>
public class OverlaySettings
{
    /// <summary>
    /// 是否启用悬浮窗
    /// </summary>
    public bool EnableOverlay { get; set; } = true;

    /// <summary>
    /// 是否窗口置顶
    /// </summary>
    public bool OverlayAlwaysOnTop { get; set; } = true;

    /// <summary>
    /// 悬浮窗字体大小
    /// </summary>
    public int OverlayFontSize { get; set; } = 16;

    /// <summary>
    /// 悬浮窗文字颜色
    /// </summary>
    public string OverlayTextColor { get; set; } = "#76B900";

    /// <summary>
    /// 悬浮窗背景颜色
    /// </summary>
    public string OverlayBackgroundColor { get; set; } = "#1a1a2eea";

    /// <summary>
    /// 悬浮窗背景透明度
    /// </summary>
    public double OverlayBackgroundOpacity { get; set; } = 0.9;

    /// <summary>
    /// 悬浮窗位置
    /// </summary>
    public OverlayPosition OverlayPosition { get; set; } = OverlayPosition.TopRight;

    /// <summary>
    /// 是否显示FPS
    /// </summary>
    public bool OverlayShowFPS { get; set; } = true;

    /// <summary>
    /// 是否显示GPU信息
    /// </summary>
    public bool OverlayShowGpu { get; set; } = true;

    /// <summary>
    /// 是否显示CPU信息
    /// </summary>
    public bool OverlayShowCpu { get; set; } = true;

    /// <summary>
    /// 是否显示内存信息
    /// </summary>
    public bool OverlayShowMemory { get; set; } = true;

    /// <summary>
    /// 是否显示延迟
    /// </summary>
    public bool OverlayShowLatency { get; set; } = true;
}

/// <summary>
/// 悬浮窗位置枚举
/// </summary>
public enum OverlayPosition
{
    /// <summary>
    /// 左上角
    /// </summary>
    TopLeft,

    /// <summary>
    /// 右上角
    /// </summary>
    TopRight,

    /// <summary>
    /// 左下角
    /// </summary>
    BottomLeft,

    /// <summary>
    /// 右下角
    /// </summary>
    BottomRight
}
