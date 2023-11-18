using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using FlomtCalibration.App.ViewModels;
using FlomtCalibration.App.Views;
using System;
using System.IO;
using System.Text.Json;

namespace FlomtCalibration.App
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var window = new MainWindow
                {
                    DataContext = new MainWindowViewModel(),
                };
                window.Opened += Window_Opened;
                window.Closing += Window_Closing;

                desktop.MainWindow = window;
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void Window_Opened(object? sender, System.EventArgs e)
        {
            var file = Environment.CurrentDirectory + "\\app_data.txt";
            if (File.Exists(file))
            {
                var json = File.ReadAllText(file);
                var vm = JsonSerializer.Deserialize<MainWindowViewModel>(json);
                if (vm != null && sender is MainWindow mainWindow && mainWindow.DataContext is MainWindowViewModel)
                {
                    mainWindow.DataContext = vm;
                }
            }
        }

        private void Window_Closing(object? sender, Avalonia.Controls.WindowClosingEventArgs e)
        {
            var file = Environment.CurrentDirectory + "\\app_data.txt";
            if (sender is MainWindow mainWindow && mainWindow.DataContext is MainWindowViewModel vm)
            {
                var json = JsonSerializer.Serialize(vm);
                File.WriteAllText(file, json);
            }
        }
    }
}