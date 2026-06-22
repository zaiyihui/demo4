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
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public void SetViewModel(PerformanceDashboardViewModel viewModel)
    {
        DataContext = viewModel;
    }

    private void OnCloseClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Hide();
    }

    private void OnMinimizeClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
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

    private void OnTimeRangeChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is PerformanceDashboardViewModel viewModel &&
            sender is ComboBox comboBox &&
            comboBox.SelectedItem is ComboBoxItem item)
        {
            viewModel.SetTimeRange(item.Content?.ToString() ?? "1小时");
        }
    }

    private void OnMetricFilterChanged(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is PerformanceDashboardViewModel viewModel &&
            sender is CheckBox checkBox)
        {
            var metric = checkBox.Tag?.ToString();
            if (!string.IsNullOrEmpty(metric))
            {
                viewModel.ToggleMetric(metric, checkBox.IsChecked == true);
            }
        }
    }

    private void OnThemeToggleClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is PerformanceDashboardViewModel viewModel)
        {
            viewModel.ToggleTheme();
        }
    }
}
