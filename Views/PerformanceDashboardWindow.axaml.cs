using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using ComputerCompanion.ViewModels;
using System;
using Avalonia.Controls.Shapes;

namespace ComputerCompanion.Views;

public partial class PerformanceDashboardWindow : Window
{
    private const double TitleBarHeight = 70;

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
        var point = e.GetCurrentPoint(this);
        if (point.Properties.IsLeftButtonPressed)
        {
            var element = e.Source as Control;
            if (element != null)
            {
                var current = element;
                while (current != null)
                {
                    if (current is Button btn && btn.Classes.Contains("window-control-button"))
                    {
                        return;
                    }
                    if (current is Border border && border.Classes.Contains("glass-header-bar"))
                    {
                        this.BeginMoveDrag(e);
                        return;
                    }
                    current = current.Parent as Control;
                }
            }
            
            var position = point.Position;
            if (position.Y <= TitleBarHeight)
            {
                this.BeginMoveDrag(e);
            }
        }
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

    private void OnMaximizeClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
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
