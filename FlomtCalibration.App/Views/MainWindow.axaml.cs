using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using FlomtCalibration.App.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace FlomtCalibration.App.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private async void OpenFileButton_Clicked(object sender, RoutedEventArgs args)
        {
            try
            {
                // Get top level from the current control. Alternatively, you can use Window reference instead.
                var topLevel = TopLevel.GetTopLevel(this);

                // Start async operation to open the dialog.
                var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = "Open File",
                    AllowMultiple = false
                });

                if (files.Count >= 1)
                {
                    if (DataContext is MainWindowViewModel vm)
                    {
                        await vm.UpdateTablesFromFile(files[0]);

                        CalibrationDataGrid.Columns.Clear();
                        var tempCount = vm.Tables.Count;
                        for (var i = 0; i < tempCount; i++)
                        {
                            CalibrationDataGrid.Columns.Add(new DataGridTextColumn
                            {
                                Header = "p (bar)",
                                Binding = new Binding($"[{i * 3}]")
                            });
                            CalibrationDataGrid.Columns.Add(new DataGridTextColumn
                            {
                                Header = "t (oC)",
                                Binding = new Binding($"[{i * 3 + 1}]")
                            });
                            CalibrationDataGrid.Columns.Add(new DataGridTextColumn
                            {
                                Header = "V (m3)",
                                Binding = new Binding($"[{i * 3 + 2}]")
                            });
                        }
                        var rows = new ObservableCollection<string[]>();

                        foreach (var (tindex, (t, pv)) in vm.Tables.Select((value, index) => (index, value)))
                        {
                            foreach (var (index, (p, v)) in pv.Select((value, index) => (index, value)))
                            {
                                var row = rows.ElementAtOrDefault(index);
                                if (row == null)
                                {
                                    row = new string[tempCount * 3];
                                    rows.Add(row);
                                }
                                row[tindex * 3] = p.ToString("0.000");
                                row[tindex * 3 + 1] = t.ToString("0.000");
                                row[tindex * 3 + 2] = v.ToString("0.000");
                            }
                        }

                        CalibrationDataGrid.ItemsSource = rows;
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        private async void SaveFileButton_Clicked(object sender, RoutedEventArgs args)
        {
            // Get top level from the current control. Alternatively, you can use Window reference instead.
            var topLevel = TopLevel.GetTopLevel(this);

            // Start async operation to open the dialog.
            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save File",
                DefaultExtension = ".csv",
            });

            if (file is not null)
            {
                (DataContext as MainWindowViewModel)?.SaveResultsToFile(file);
            }
        }
    }
}