using ComputerCompanion.Models;

namespace ComputerCompanion.Services;

public interface ISettingsService
{
    Settings GetSettings();
    void SaveSettings();
    void LoadSettings();
    void ResetToDefaults();
    void UpdateSettingsPath(string newPath);
}