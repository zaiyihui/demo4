namespace ComputerCompanion.Models;

public class Settings
{
    public LayoutMode LayoutMode { get; set; } = LayoutMode.Vertical;
    public string TextColor { get; set; } = "#FFFFFF";
    public string BackgroundColor { get; set; } = "#1a1a2eea";
    public double BackgroundOpacity { get; set; } = 0.9;
    public int FontSize { get; set; } = 14;
    public int RefreshInterval { get; set; } = 1000;
    public bool GameMode { get; set; } = false;
    public int GameModeRefreshInterval { get; set; } = 3000;
    
    public bool ShowCpu { get; set; } = true;
    public bool ShowGpu { get; set; } = true;
    public bool ShowMemory { get; set; } = true;
    public bool ShowNetwork { get; set; } = true;
    public bool ShowDisk { get; set; } = true;
    public bool ShowBattery { get; set; } = true;
    
    public int WindowX { get; set; } = 100;
    public int WindowY { get; set; } = 100;
    
    public bool EnableOverlay { get; set; } = true;
    public bool OverlayAlwaysOnTop { get; set; } = true;
    public int OverlayFontSize { get; set; } = 16;
    public string OverlayTextColor { get; set; } = "#76B900";
    public OverlayPosition OverlayPosition { get; set; } = OverlayPosition.TopRight;
    public bool OverlayShowFPS { get; set; } = true;
    public bool OverlayShowGpu { get; set; } = true;
    public bool OverlayShowCpu { get; set; } = true;
    public bool OverlayShowMemory { get; set; } = true;
    public bool OverlayShowLatency { get; set; } = true;
    
    public bool AutoStart { get; set; } = false;
    public bool StartMinimized { get; set; } = false;
}

public enum LayoutMode
{
    Vertical,
    Horizontal
}

public enum OverlayPosition
{
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight
}