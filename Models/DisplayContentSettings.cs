namespace ComputerCompanion.Models;

/// <summary>
/// 主窗口显示内容设置
/// </summary>
public class DisplayContentSettings
{
    /// <summary>
    /// 是否显示CPU信息
    /// </summary>
    public bool ShowCpu { get; set; } = true;

    /// <summary>
    /// 是否显示GPU信息
    /// </summary>
    public bool ShowGpu { get; set; } = true;

    /// <summary>
    /// 是否显示内存信息
    /// </summary>
    public bool ShowMemory { get; set; } = true;

    /// <summary>
    /// 是否显示网络信息
    /// </summary>
    public bool ShowNetwork { get; set; } = true;

    /// <summary>
    /// 是否显示磁盘信息
    /// </summary>
    public bool ShowDisk { get; set; } = true;

    /// <summary>
    /// 是否显示电池信息
    /// </summary>
    public bool ShowBattery { get; set; } = true;
}
