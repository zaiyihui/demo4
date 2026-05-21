using Avalonia.Controls;
using ComputerCompanion.Services;
using ComputerCompanion.ViewModels;
using System;

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

    private void OnSaveClick(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
    }
}
