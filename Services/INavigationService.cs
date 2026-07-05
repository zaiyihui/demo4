using Avalonia.Controls;
using System;

namespace ComputerCompanion.Services;

public interface INavigationService
{
    Window? MainWindow { get; }

    void SetMainWindow(Window window);

    void ShowSettings();

    void ShowPerformanceDashboard();

    void ShowMainWindow();

    void HideMainWindow();

    void ToggleMainWindow();

    void ClosePerformanceDashboard();

    void CloseAll();

    void SwitchOverlayViewMode();
}