using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Styling;
using System;
using System.Collections.Generic;

namespace ComputerCompanion.Services;

public enum ThemeMode
{
    Dark,
    Light,
    System
}

public interface IThemeService
{
    ThemeMode CurrentTheme { get; }
    void SetTheme(ThemeMode mode);
    void ToggleTheme();
    event EventHandler<ThemeMode>? ThemeChanged;
}

public class ThemeService : IThemeService
{
    private ThemeMode _currentTheme;
    private readonly ISettingsService _settingsService;

    public ThemeMode CurrentTheme
    {
        get => _currentTheme;
        private set
        {
            if (_currentTheme != value)
            {
                _currentTheme = value;
                ThemeChanged?.Invoke(this, value);
                _settingsService.SaveThemeMode(value);
            }
        }
    }

    public event EventHandler<ThemeMode>? ThemeChanged;

    public ThemeService(ISettingsService settingsService)
    {
        _settingsService = settingsService;
        _currentTheme = _settingsService.LoadThemeMode();
    }

    public void SetTheme(ThemeMode mode)
    {
        CurrentTheme = mode;
        ApplyTheme();
    }

    public void ToggleTheme()
    {
        switch (CurrentTheme)
        {
            case ThemeMode.Dark:
                SetTheme(ThemeMode.Light);
                break;
            case ThemeMode.Light:
                SetTheme(ThemeMode.Dark);
                break;
            case ThemeMode.System:
                SetTheme(ThemeMode.Dark);
                break;
        }
    }

    private void ApplyTheme()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var styles = Application.Current.Styles;
            
            foreach (var style in styles)
            {
                if (style is IStyle)
                {
                    UpdateStyleResources(style);
                }
            }
        }
    }

    private void UpdateStyleResources(IStyle style)
    {
        if (Application.Current == null) return;

        var resources = Application.Current.Resources;

        if (CurrentTheme == ThemeMode.Dark)
        {
            resources["WindowBackground"] = new SolidColorBrush(Color.FromRgb(10, 10, 20));
            resources["CardBackground"] = new SolidColorBrush(Color.FromRgb(21, 21, 32));
            resources["CardBorder"] = new SolidColorBrush(Color.FromRgb(37, 37, 48));
            resources["TextPrimary"] = new SolidColorBrush(Color.FromRgb(255, 255, 255));
            resources["TextSecondary"] = new SolidColorBrush(Color.FromRgb(136, 136, 136));
            resources["TextTertiary"] = new SolidColorBrush(Color.FromRgb(102, 102, 102));
            resources["GridLine"] = new SolidColorBrush(Color.FromRgb(37, 37, 48));
        }
        else
        {
            resources["WindowBackground"] = new SolidColorBrush(Color.FromRgb(245, 245, 245));
            resources["CardBackground"] = new SolidColorBrush(Color.FromRgb(255, 255, 255));
            resources["CardBorder"] = new SolidColorBrush(Color.FromRgb(200, 200, 200));
            resources["TextPrimary"] = new SolidColorBrush(Color.FromRgb(30, 30, 30));
            resources["TextSecondary"] = new SolidColorBrush(Color.FromRgb(100, 100, 100));
            resources["TextTertiary"] = new SolidColorBrush(Color.FromRgb(150, 150, 150));
            resources["GridLine"] = new SolidColorBrush(Color.FromRgb(200, 200, 200));
        }
    }
}
