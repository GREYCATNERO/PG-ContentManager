using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace AnimeTagsEditor
{
    public partial class WelcomeWindow : Window
    {
        private readonly List<string> _recentFiles = new();
        private const string RecentFilesPath = "recent_files.txt";

        public WelcomeWindow()
        {
            InitializeComponent();
            LoadRecentFiles();
        }

        private void LoadRecentFiles()
        {
            RecentFilesPanel.Children.Clear();

            if (File.Exists(RecentFilesPath))
            {
                var lines = File.ReadAllLines(RecentFilesPath);
                foreach (var line in lines)
                {
                    if (!string.IsNullOrWhiteSpace(line) && File.Exists(line))
                    {
                        _recentFiles.Add(line);
                    }
                }
            }

            if (_recentFiles.Count == 0)
            {
                RecentFilesPanel.Children.Add(new TextBlock
                {
                    Text = "Нет недавних файлов",
                    Foreground = new SolidColorBrush(Color.FromRgb(102, 102, 102)),
                    FontSize = 12,
                    FontStyle = FontStyles.Italic
                });
            }
            else
            {
                foreach (var file in _recentFiles)
                {
                    var btn = new Button
                    {
                        Content = Path.GetFileName(file),
                        Background = new SolidColorBrush(Color.FromRgb(37, 37, 37)),
                        BorderBrush = new SolidColorBrush(Color.FromRgb(68, 68, 68)),
                        Foreground = Brushes.White,
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        Margin = new Thickness(0, 0, 0, 5),
                        Padding = new Thickness(10, 5, 10, 5),
                        Cursor = Cursors.Hand,
                        Tag = file,
                        HorizontalContentAlignment = HorizontalAlignment.Left
                    };
                    btn.Click += (s, e) => OpenRecentFile(btn.Tag?.ToString());
                    RecentFilesPanel.Children.Add(btn);
                }
            }
        }

        private void SaveRecentFiles()
        {
            try
            {
                File.WriteAllLines(RecentFilesPath, _recentFiles);
            }
            catch { }
        }

        private void AddToRecent(string filePath)
        {
            _recentFiles.Remove(filePath);
            _recentFiles.Insert(0, filePath);
            if (_recentFiles.Count > 5)
                _recentFiles.RemoveRange(5, _recentFiles.Count - 5);
            SaveRecentFiles();
        }

        // ✅ Перетаскивание окна
        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void CreateNew_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "JSON Files (*.json)|*.json",
                FileName = "AnimeTags_New.json",
                Title = "Создать новый JSON файл"
            };

            if (dialog.ShowDialog() == true)
            {
                var mainWindow = new MainWindow(dialog.FileName);
                mainWindow.Show();
                Close();
            }
        }

        private void OpenExisting_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog { Filter = "JSON Files (*.json)|*.json" };
            if (dialog.ShowDialog() == true)
            {
                AddToRecent(dialog.FileName);
                var mainWindow = new MainWindow(dialog.FileName);
                mainWindow.Show();
                Close();
            }
        }

        private void OpenRecentFile(string? filePath)
        {
            if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
            {
                AddToRecent(filePath);
                var mainWindow = new MainWindow(filePath);
                mainWindow.Show();
                Close();
            }
            else
            {
                MessageBox.Show("Файл не найден!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                _recentFiles.Remove(filePath ?? "");
                SaveRecentFiles();
                LoadRecentFiles();
            }
        }

        private void OpenEditor_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = new MainWindow();
            mainWindow.Show();
            Close();
        }
    }
}