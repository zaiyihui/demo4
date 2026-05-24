using System;

namespace ComputerCompanion.Services;

public class TrayIconService : IDisposable
{
    private readonly SettingsService _settingsService;

    public TrayIconService(SettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public void Initialize()
    {
    }

    public void Dispose()
    {
    }
}