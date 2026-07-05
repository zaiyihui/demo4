using ComputerCompanion.Services;

namespace ComputerCompanion.Models;

public class PerformanceSettings
{
    public int RefreshInterval { get; set; } = 1000;

    public ThemeMode ThemeMode { get; set; } = ThemeMode.Dark;

    public bool AutoBackupEnabled { get; set; } = true;

    public int AutoBackupIntervalHours { get; set; } = 24;

    public bool DifferentialBackupEnabled { get; set; } = false;
}