using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using ComputerCompanion.Models;
using ComputerCompanion.Services;
using ComputerCompanion.ViewModels;
using System;
using System.Globalization;

namespace ComputerCompanion.Views;

public partial class SettingsWindow : Window
{
    private readonly Settings _settings;
    private readonly Action<Settings> _onSave;

    public SettingsWindow(Settings settings, Action<Settings> onSave)
    {
        InitializeComponent();
        _settings = settings;
        _onSave = onSave;
        DataContext = new SettingsViewModel(settings, onSave);
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel viewModel)
        {
            viewModel.Save();
        }
        _onSave?.Invoke(_settings);
        Close();
    }

    private void CancelWindow(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void MinimizeWindow(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void MaximizeWindow(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    private void CloseWindow(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void NavItem_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton radioButton && radioButton.Tag is string tag)
        {
            OverviewPanel.IsVisible = false;
            DisplayPanel.IsVisible = false;
            AppearancePanel.IsVisible = false;
            ContentPanel.IsVisible = false;
            PerformancePanel.IsVisible = false;
            StartupPanel.IsVisible = false;

            switch (tag)
            {
                case "overview":
                    OverviewPanel.IsVisible = true;
                    break;
                case "display":
                    DisplayPanel.IsVisible = true;
                    break;
                case "appearance":
                    AppearancePanel.IsVisible = true;
                    break;
                case "content":
                    ContentPanel.IsVisible = true;
                    break;
                case "performance":
                    PerformancePanel.IsVisible = true;
                    break;
                case "startup":
                    StartupPanel.IsVisible = true;
                    break;
            }
        }
    }

    private void ColorPreset_Clicked(object sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        if (sender is Border border && border.Tag is string color)
        {
            if (DataContext is SettingsViewModel viewModel)
            {
                viewModel.OverlayTextColor = color;
            }
        }
    }

    private void ShowColorPresets(object sender, RoutedEventArgs e)
    {
    }
}
