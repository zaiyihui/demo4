using System.Collections.Generic;
using ComputerCompanion.Models;

namespace ComputerCompanion.Services;

public interface ISettingsService
{
    Settings GetSettings();
    void SaveSettings();
    void LoadSettings();
    void ResetToDefaults();
    void UpdateSettingsPath(string newPath);
    ThemeMode LoadThemeMode();
    void SaveThemeMode(ThemeMode mode);
    List<AlertRule> LoadAlertRules();
    void SaveAlertRules(List<AlertRule> rules);
}