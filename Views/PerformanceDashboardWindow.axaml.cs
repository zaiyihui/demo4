using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ComputerCompanion.ViewModels;
using System;

namespace ComputerCompanion.Views;

public partial class PerformanceDashboardWindow : Window
{


    public PerformanceDashboardWindow()
    {
        InitializeComponent();
        KeyDown += OnKeyDown;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public void SetViewModel(PerformanceDashboardViewModel viewModel)
    {
        DataContext = viewModel;
    }

    private void OnKeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
    {
        if (e.Key == Avalonia.Input.Key.F3)
        {
            OnOpenSettings(null, null);
        }
    }

    #region 窗口控制方法

    private void OnTitleBarPointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        this.BeginMoveDrag(e);
    }

    private void OnTitleBarDoubleTapped(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (WindowState == WindowState.Maximized)
        {
            WindowState = WindowState.Normal;
        }
        else
        {
            WindowState = WindowState.Maximized;
        }
    }

    private void OnMinimizeClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void OnCloseClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
    }

    #endregion

    #region 功能按钮事件

    private void OnOpenSettings(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        try
        {
            var navigationService = App.ServiceProvider.GetService(typeof(Services.INavigationService)) as Services.INavigationService;
            navigationService?.ShowSettings();
        }
        catch (Exception ex)
        {
            Program.Log($"[性能面板] OnOpenSettings 失败: {ex.Message}");
        }
    }

    private void OnRefreshClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is PerformanceDashboardViewModel viewModel)
        {
            _ = viewModel.RefreshDataAsync();
        }
    }

    private void OnExportClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is PerformanceDashboardViewModel viewModel)
        {
            _ = viewModel.ExportDataAsync();
        }
    }

    private void OnThemeToggleClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is PerformanceDashboardViewModel viewModel)
        {
            viewModel.ToggleTheme();
        }
    }

    private void OnToggleRecording(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is PerformanceDashboardViewModel viewModel)
        {
            viewModel.ToggleRecording();
        }
    }

    private void OnTimeRangeChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is PerformanceDashboardViewModel viewModel &&
            sender is ComboBox comboBox &&
            comboBox.SelectedItem is ComboBoxItem item)
        {
            viewModel.SetTimeRange(item.Content?.ToString() ?? "1小时");
        }
    }

    private void OnMetricCardClicked(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        if (DataContext is PerformanceDashboardViewModel viewModel &&
            sender is Border border &&
            border.Tag is string cardName)
        {
            viewModel.SelectMetricCard(cardName);
        }
    }

    #endregion
}
