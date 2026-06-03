using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using ComputerCompanion.Models;
using ComputerCompanion.Services;
using ComputerCompanion.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace ComputerCompanion.Views;

public partial class SettingsWindow : Window
{
    private readonly Settings _settings;
    private readonly Action<Settings> _onSave;
    private ColorPresetService? _colorPresetService;

    public SettingsWindow(Settings settings, Action<Settings> onSave)
    {
        InitializeComponent();
        _settings = settings;
        _onSave = onSave;
        DataContext = new SettingsViewModel(settings, onSave);

        InitializeColorPresetService();
    }

    private void InitializeColorPresetService()
    {
        try
        {
            _colorPresetService = new ColorPresetService();
            Program.Log("[设置] 颜色预设服务初始化成功");
        }
        catch (Exception ex)
        {
            Program.Log($"[设置] 初始化颜色预设服务失败: {ex.Message}");
        }
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel viewModel)
        {
            viewModel.Save();

            // 应用颜色预设
            if (_colorPresetService != null && viewModel.OverlayTextColor != null)
            {
                var currentPreset = _colorPresetService.GetCurrentPreset();
                if (currentPreset != null)
                {
                    currentPreset.TextColor = viewModel.OverlayTextColor;
                    currentPreset.BackgroundColor = viewModel.BackgroundColor;
                    currentPreset.BackgroundOpacity = viewModel.BackgroundOpacity;
                }
            }
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

                // 更新颜色预览
                Program.Log($"[设置] 颜色已更改为: {color}");

                // 记录使用预设
                if (_colorPresetService != null)
                {
                    var preset = _colorPresetService.GetAllPresets()
                        .FirstOrDefault(p => p.TextColor == color && p.IsSystemPreset);

                    if (preset != null)
                    {
                        _colorPresetService.SetCurrentPreset(preset.Id);
                        Program.Log($"[设置] 已应用预设: {preset.Name}");
                    }
                }
            }
        }
    }

    private void ShowColorPresets(object sender, RoutedEventArgs e)
    {
        if (_colorPresetService == null)
        {
            Program.Log("[设置] 颜色预设服务未初始化");
            return;
        }

        // 获取所有预设
        var presets = _colorPresetService.GetAllPresets();

        // 按类别分组显示（UI展示由XAML绑定处理）
        _ = presets;
    }

    /// <summary>
    /// 应用指定的颜色预设
    /// </summary>
    public void ApplyColorPreset(string presetId)
    {
        if (_colorPresetService == null || DataContext is not SettingsViewModel viewModel)
            return;

        var preset = _colorPresetService.GetAllPresets()
            .FirstOrDefault(p => p.Id == presetId);

        if (preset != null)
        {
            viewModel.OverlayTextColor = preset.TextColor;
            viewModel.BackgroundColor = preset.BackgroundColor;
            viewModel.BackgroundOpacity = preset.BackgroundOpacity;

            _colorPresetService.SetCurrentPreset(presetId);

            Console.WriteLine($"已应用颜色预设: {preset.Name}");
        }
    }

    /// <summary>
    /// 创建自定义预设
    /// </summary>
    public ColorPreset? CreateCustomPreset(string name, string description, string textColor, string backgroundColor, double opacity)
    {
        if (_colorPresetService == null)
            return null;

        var preset = new ColorPreset
        {
            Name = name,
            Description = description,
            TextColor = textColor,
            BackgroundColor = backgroundColor,
            BackgroundOpacity = opacity,
            Category = ColorPresetCategory.Custom,
            IsSystemPreset = false
        };

        return _colorPresetService.AddPreset(preset);
    }

    /// <summary>
    /// 导出自定义预设
    /// </summary>
    public bool ExportPresets(string filePath)
    {
        if (_colorPresetService == null)
            return false;

        return _colorPresetService.ExportPresets(filePath);
    }

    /// <summary>
    /// 导入预设
    /// </summary>
    public int ImportPresets(string filePath)
    {
        if (_colorPresetService == null)
            return 0;

        return _colorPresetService.ImportPresets(filePath);
    }

    /// <summary>
    /// 获取当前预设
    /// </summary>
    public ColorPreset? GetCurrentPreset()
    {
        return _colorPresetService?.GetCurrentPreset();
    }
}
