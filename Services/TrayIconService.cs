using System;

namespace ComputerCompanion.Services;

public class TrayIconService : IDisposable
{
    private readonly SettingsService _settingsService;
    private bool _isDisposed;

    public TrayIconService(SettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public void Initialize()
    {
    }

    public void Dispose()
    {
        _isDisposed = true;
    }

    public event EventHandler? ShowMainWindow;
    public event EventHandler? ExitApplication;
}