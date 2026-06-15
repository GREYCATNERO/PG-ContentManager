using AnimeTagsEditor.Models;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace AnimeTagsEditor
{
    public partial class MainWindow : Window
    {
        private TagsData? _data;
        private string? _currentFilePath;
        private Tag? _selectedTag;
        private string? _selectedCategory;

        private readonly Dictionary<string, List<Tag>> _customCategories = new();

        private readonly Dictionary<string, Func<TagsData, List<Tag>>> _categoryMap = new()
        {
            ["main_genres"] = d => d.MainGenres,
            ["demographics"] = d => d.Demographics,
            ["isekai_fantasy"] = d => d.IsekaiFantasy,
            ["sci_fi_tech"] = d => d.SciFiTech,
            ["school_life"] = d => d.SchoolLife,
            ["military_war"] = d => d.MilitaryWar,
            ["music_arts"] = d => d.MusicArts,
            ["relationships_romance"] = d => d.RelationshipsRomance,
            ["character_archetypes"] = d => d.CharacterArchetypes,
            ["plot_structures"] = d => d.PlotStructures,
            ["tone_mood"] = d => d.ToneMood,
            ["content_warnings"] = d => d.ContentWarnings,
            ["niche_aesthetics"] = d => d.NicheAesthetics,
            ["production_format"] = d => d.ProductionFormat,
            ["historical_periods"] = d => d.HistoricalPeriods,
            ["power_systems"] = d => d.PowerSystems
        };

        private readonly Dictionary<string, string> _categoryNamesRu = new()
        {
            ["main_genres"] = "Основные жанры",
            ["demographics"] = "Демография",
            ["isekai_fantasy"] = "Исекай и Фэнтези",
            ["sci_fi_tech"] = "Научная фантастика",
            ["school_life"] = "Школьная жизнь",
            ["military_war"] = "Военное / Война",
            ["music_arts"] = "Музыка и Искусство",
            ["relationships_romance"] = "Романтика",
            ["character_archetypes"] = "Архетипы персонажей",
            ["plot_structures"] = "Структуры сюжета",
            ["tone_mood"] = "Настроение и Атмосфера",
            ["content_warnings"] = "Предупреждения",
            ["niche_aesthetics"] = "Нишевая эстетика",
            ["production_format"] = "Формат производства",
            ["historical_periods"] = "Исторические эпохи",
            ["power_systems"] = "Системы сил"
        };

        private string _previewSearchQuery = "";
        private string _previewSelectedCategory = "all";

        // ✅ ИСПРАВЛЕННЫЙ конструктор БЕЗ параметров
        public MainWindow()
        {
            InitializeComponent();
            InitializeCategoryCombo();
            InitializeContactCards();

            // Создаём пустую структуру данных
            _data = new TagsData
            {
                MainGenres = new List<Tag>(),
                Demographics = new List<Tag>(),
                IsekaiFantasy = new List<Tag>(),
                SciFiTech = new List<Tag>(),
                SchoolLife = new List<Tag>(),
                MilitaryWar = new List<Tag>(),
                MusicArts = new List<Tag>(),
                RelationshipsRomance = new List<Tag>(),
                CharacterArchetypes = new List<Tag>(),
                PlotStructures = new List<Tag>(),
                ToneMood = new List<Tag>(),
                ContentWarnings = new List<Tag>(),
                NicheAesthetics = new List<Tag>(),
                ProductionFormat = new List<Tag>(),
                HistoricalPeriods = new List<Tag>(),
                PowerSystems = new List<Tag>()
            };

            PopulateUI();
        }

        // ✅ НОВЫЙ конструктор С параметром (для открытия файла)
        public MainWindow(string filePath) : this()
        {
            if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
            {
                try
                {
                    _currentFilePath = filePath;
                    var json = File.ReadAllText(filePath);
                    _data = JsonSerializer.Deserialize<TagsData>(json);

                    if (CurrentFileName != null)
                        CurrentFileName.Text = Path.GetFileName(filePath);

                    RenderPreviewTags();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки файла: {ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        #region 🔴 НОВАЯ ФУНКЦИЯ: Выход в меню

        private void ExitToMenu_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Выйти в главное меню?\n\nНесохранённые изменения будут потеряны!",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                var welcomeWindow = new WelcomeWindow();
                welcomeWindow.Show();
                Close();
            }
        }

        #endregion

        #region Вкладки 1 и 2: Просмотр и Редактирование

        private void InitializeCategoryCombo()
        {
            if (CategoryCombo == null) return;
            CategoryCombo.Items.Clear();

            foreach (var key in _categoryMap.Keys)
            {
                if (_categoryNamesRu.TryGetValue(key, out var niceName))
                    CategoryCombo.Items.Add(niceName);
                else
                    CategoryCombo.Items.Add(key);
            }

            foreach (var key in _customCategories.Keys)
                CategoryCombo.Items.Add(key);
        }

        private void OpenJson_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog { Filter = "JSON Files (*.json)|*.json" };
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var json = File.ReadAllText(dialog.FileName);
                    _data = JsonSerializer.Deserialize<TagsData>(json);
                    _currentFilePath = dialog.FileName;

                    if (CurrentFileName != null)
                        CurrentFileName.Text = System.IO.Path.GetFileName(_currentFilePath);

                    PopulateUI();
                    InitializeCategoryCombo();
                    RenderPreviewTags();
                    MessageBox.Show("Файл загружен успешно!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void PopulateUI()
        {
            if (_data == null) return;

            TagsTree.Items.Clear();

            foreach (var kvp in _categoryMap)
            {
                string displayName = _categoryNamesRu.TryGetValue(kvp.Key, out var name) ? name : kvp.Key;
                var catItem = new TreeViewItem { Header = displayName, FontWeight = FontWeights.Bold };
                var tags = kvp.Value(_data);
                foreach (var tag in tags)
                    catItem.Items.Add(new TreeViewItem { Header = $"{tag.NameRu} ({tag.Slug})", Tag = tag });

                if (tags.Count > 0) TagsTree.Items.Add(catItem);
            }

            foreach (var kvp in _customCategories)
            {
                var catItem = new TreeViewItem { Header = kvp.Key, FontWeight = FontWeights.Bold };
                foreach (var tag in kvp.Value)
                    catItem.Items.Add(new TreeViewItem { Header = $"{tag.NameRu} ({tag.Slug})", Tag = tag });

                if (kvp.Value.Count > 0) TagsTree.Items.Add(catItem);
            }

            if (CategoryCombo.Items.Count > 0 && CategoryCombo.SelectedIndex == -1)
                CategoryCombo.SelectedIndex = 0;
        }

        private void TagsTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is TreeViewItem item && item.Tag is Tag tag)
                ShowPreview(tag);
        }

        private void ShowPreview(Tag tag)
        {
            if (tag == null || PreviewPanel == null) return;
            PreviewPanel.Children.Clear();

            PreviewPanel.Children.Add(CreateTextBlock(tag.NameRu, 24, Brushes.Gold, FontWeights.Bold));
            PreviewPanel.Children.Add(CreateTextBlock($"{tag.NameEn} | {tag.Slug}", 14, Brushes.LightGray));
            PreviewPanel.Children.Add(new Separator { Margin = new Thickness(0, 10, 0, 10) });

            PreviewPanel.Children.Add(CreateTextBlock("📝 Кратко:", 16, Brushes.LimeGreen, FontWeights.Bold));
            PreviewPanel.Children.Add(CreateTextBlock(tag.ShortDesc ?? "", 13, Brushes.White));
            PreviewPanel.Children.Add(new Separator { Margin = new Thickness(0, 10, 0, 10) });

            if (!string.IsNullOrEmpty(tag.Wiki?.Overview))
            {
                PreviewPanel.Children.Add(CreateTextBlock("📖 Описание:", 16, Brushes.LimeGreen, FontWeights.Bold));
                PreviewPanel.Children.Add(CreateTextBlock(tag.Wiki.Overview, 13, Brushes.White));
                PreviewPanel.Children.Add(new Separator { Margin = new Thickness(0, 10, 0, 10) });
            }

            if (tag.Wiki?.KeyTraits != null && tag.Wiki.KeyTraits.Count > 0)
            {
                PreviewPanel.Children.Add(CreateTextBlock("✨ Черты:", 16, Brushes.LimeGreen, FontWeights.Bold));
                foreach (var t in tag.Wiki.KeyTraits)
                    PreviewPanel.Children.Add(CreateTextBlock($"• {t}", 12, Brushes.White));
                PreviewPanel.Children.Add(new Separator { Margin = new Thickness(0, 10, 0, 10) });
            }

            if (tag.Wiki?.ForWho != null && tag.Wiki.ForWho.Count > 0)
            {
                PreviewPanel.Children.Add(CreateTextBlock("👥 Для кого:", 16, Brushes.LimeGreen, FontWeights.Bold));
                foreach (var w in tag.Wiki.ForWho)
                    PreviewPanel.Children.Add(CreateTextBlock($"✓ {w}", 12, Brushes.White));
                PreviewPanel.Children.Add(new Separator { Margin = new Thickness(0, 10, 0, 10) });
            }

            if (tag.Wiki?.Examples != null && tag.Wiki.Examples.Count > 0)
            {
                PreviewPanel.Children.Add(CreateTextBlock("🎬 Примеры:", 16, Brushes.LimeGreen, FontWeights.Bold));
                int i = 1;
                foreach (var ex in tag.Wiki.Examples)
                {
                    PreviewPanel.Children.Add(CreateTextBlock($"#{i} {ex.Title}", 13, Brushes.Cyan));
                    PreviewPanel.Children.Add(CreateTextBlock($"   {ex.Reason}", 11, Brushes.LightGray));
                    i++;
                }
            }
        }

        private TextBlock CreateTextBlock(string text, double size, Brush color, FontWeight? weight = null)
        {
            return new TextBlock
            {
                Text = text ?? "",
                FontSize = size,
                Foreground = color,
                FontWeight = weight ?? FontWeights.Normal,
                Margin = new Thickness(0, 2, 0, 2),
                TextWrapping = TextWrapping.Wrap
            };
        }

        private void CreateNew_Click(object sender, RoutedEventArgs e)
        {
            var saveDialog = new SaveFileDialog
            {
                Filter = "JSON Files (*.json)|*.json",
                FileName = "AnimeTags_New.json",
                Title = "Создать новый JSON файл"
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    _data = new TagsData();
                    _currentFilePath = saveDialog.FileName;

                    if (CurrentFileName != null)
                        CurrentFileName.Text = System.IO.Path.GetFileName(_currentFilePath);

                    InitializeCategoryCombo();
                    RenderPreviewTags();

                    MessageBox.Show("Новый файл создан! Теперь можете добавлять теги.", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка создания файла: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void CategoryCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_data == null && _customCategories.Count == 0) return;
            if (CategoryCombo.SelectedItem == null) return;

            string selectedText = CategoryCombo.SelectedItem.ToString() ?? "";
            string selectedKey = _categoryNamesRu.FirstOrDefault(x => x.Value == selectedText).Key;
            if (string.IsNullOrEmpty(selectedKey))
                selectedKey = selectedText;

            _selectedCategory = selectedKey;
            TagsList.Items.Clear();

            if (_data != null && _categoryMap.TryGetValue(selectedKey, out var getList))
            {
                foreach (var tag in getList(_data))
                    TagsList.Items.Add(tag);
            }
            else if (_customCategories.TryGetValue(selectedKey, out var customList))
            {
                foreach (var tag in customList)
                    TagsList.Items.Add(tag);
            }
        }

        private void TagsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TagsList.SelectedItem is Tag tag)
            {
                _selectedTag = tag;
                ShowEditForm(tag);
            }
        }

        private void ShowEditForm(Tag tag)
        {
            if (tag == null || EditForm == null) return;
            EditForm.Children.Clear();

            EditForm.Children.Add(CreateLabelWithTooltip("Slug:", "Уникальный идентификатор тега на английском (например: horror, romance, isekai)"));
            EditForm.Children.Add(CreateTextBox(tag.Slug ?? "", s => tag.Slug = s));

            EditForm.Children.Add(CreateLabelWithTooltip("Name EN:", "Название тега на английском языке"));
            EditForm.Children.Add(CreateTextBox(tag.NameEn ?? "", s => tag.NameEn = s));

            EditForm.Children.Add(CreateLabelWithTooltip("Name RU:", "Название тега на русском языке"));
            EditForm.Children.Add(CreateTextBox(tag.NameRu ?? "", s => tag.NameRu = s));

            EditForm.Children.Add(CreateLabelWithTooltip("Short Description:", "Краткое описание тега (1-2 предложения). Показывается в списке тегов"));
            EditForm.Children.Add(CreateTextBox(tag.ShortDesc ?? "", s => tag.ShortDesc = s, true));

            EditForm.Children.Add(CreateLabelWithTooltip("Wiki Overview:", "Подробное описание тега для википедии"));
            EditForm.Children.Add(CreateTextBox(tag.Wiki?.Overview ?? "", s => { if (tag.Wiki != null) tag.Wiki.Overview = s; }, true));

            EditForm.Children.Add(CreateLabelWithTooltip("Key Traits:", "Ключевые черты жанра. Каждая черта с новой строки"));
            EditForm.Children.Add(CreateTextBox(
                tag.Wiki?.KeyTraits != null ? string.Join("\n", tag.Wiki.KeyTraits) : "",
                s => { if (tag.Wiki != null) tag.Wiki.KeyTraits = s.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList(); },
                true));

            EditForm.Children.Add(CreateLabelWithTooltip("For Who:", "Для кого этот жанр. Каждый пункт с новой строки"));
            EditForm.Children.Add(CreateTextBox(
                tag.Wiki?.ForWho != null ? string.Join("\n", tag.Wiki.ForWho) : "",
                s => { if (tag.Wiki != null) tag.Wiki.ForWho = s.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList(); },
                true));

            EditForm.Children.Add(CreateLabelWithTooltip("Related Tags:", "Похожие теги через запятую. Пример: action, adventure"));
            EditForm.Children.Add(CreateTextBox(
                tag.Wiki?.Related != null ? string.Join(", ", tag.Wiki.Related) : "",
                s => { if (tag.Wiki != null) tag.Wiki.Related = s.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToList(); },
                true));

            EditForm.Children.Add(CreateLabelWithTooltip("Warnings:", "Предупреждения о содержимом. Каждое с новой строки"));
            EditForm.Children.Add(CreateTextBox(
                tag.Wiki?.Warnings != null ? string.Join("\n", tag.Wiki.Warnings) : "",
                s => { if (tag.Wiki != null) tag.Wiki.Warnings = s.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList(); },
                true));

            EditForm.Children.Add(new TextBlock
            {
                Text = "⚠️ Примеры аниме (Examples) редактируются напрямую в JSON для сохранения структуры.",
                Foreground = Brushes.Orange,
                Margin = new Thickness(0, 15, 0, 0),
                TextWrapping = TextWrapping.Wrap
            });
        }

        private StackPanel CreateLabelWithTooltip(string labelText, string tooltip)
        {
            var panel = new StackPanel();
            panel.Children.Add(new TextBlock
            {
                Text = labelText,
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 10, 0, 2),
                FontSize = 13
            });
            panel.Children.Add(new TextBlock
            {
                Text = tooltip,
                Foreground = new SolidColorBrush(Color.FromRgb(150, 150, 150)),
                FontSize = 11,
                FontStyle = FontStyles.Italic,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 5)
            });
            return panel;
        }

        private TextBox CreateTextBox(string text, Action<string> onChanged, bool multiline = false)
        {
            var tb = new TextBox
            {
                Text = text ?? "",
                Background = new SolidColorBrush(Color.FromArgb(255, 50, 50, 50)),
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 5),
                Padding = new Thickness(5),
                AcceptsReturn = multiline,
                TextWrapping = multiline ? TextWrapping.Wrap : TextWrapping.NoWrap,
                Height = multiline ? 80 : 25,
                VerticalAlignment = VerticalAlignment.Top
            };
            tb.TextChanged += (s, e) => onChanged(tb.Text);
            return tb;
        }

        private void AddTag_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCategory == null) { MessageBox.Show("Сначала выберите категорию!"); return; }

            var newTag = new Tag { Slug = "new_tag", NameEn = "New Tag", NameRu = "Новый тег", ShortDesc = "Описание", Wiki = new Wiki() };

            if (_data != null && _categoryMap.TryGetValue(_selectedCategory, out var getList))
            {
                getList(_data).Add(newTag);
                TagsList.Items.Add(newTag);
                TagsList.SelectedItem = newTag;
            }
            else if (_customCategories.TryGetValue(_selectedCategory, out var customList))
            {
                customList.Add(newTag);
                TagsList.Items.Add(newTag);
                TagsList.SelectedItem = newTag;
            }
            else
            {
                MessageBox.Show("Категория не найдена!");
            }
        }

        private void DeleteTag_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedTag == null || _selectedCategory == null) { MessageBox.Show("Выберите тег для удаления!"); return; }

            if (MessageBox.Show($"Удалить тег '{_selectedTag.NameRu}'?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                if (_data != null && _categoryMap.TryGetValue(_selectedCategory, out var getList))
                {
                    getList(_data).Remove(_selectedTag);
                    TagsList.Items.Remove(_selectedTag);
                }
                else if (_customCategories.TryGetValue(_selectedCategory, out var customList))
                {
                    customList.Remove(_selectedTag);
                    TagsList.Items.Remove(_selectedTag);
                }

                _selectedTag = null;
                EditForm.Children.Clear();
            }
        }

        private void SaveJson_Click(object sender, RoutedEventArgs e)
        {
            if (_data == null) { MessageBox.Show("Нет данных для сохранения!"); return; }

            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };

                var json = JsonSerializer.Serialize(_data, options);

                var saveDialog = new SaveFileDialog { Filter = "JSON Files (*.json)|*.json", FileName = _currentFilePath ?? "AnimeTags_Edited.json" };
                if (saveDialog.ShowDialog() == true)
                {
                    File.WriteAllText(saveDialog.FileName, json);
                    _currentFilePath = saveDialog.FileName;
                    if (CurrentFileName != null)
                        CurrentFileName.Text = System.IO.Path.GetFileName(_currentFilePath);
                    MessageBox.Show("Сохранено успешно!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddCategory_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new TextInputDialog("Введите название новой категории:", "Добавить категорию");
            if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.Response))
            {
                var categoryName = dialog.Response.Trim().ToLower().Replace(' ', '_');

                if (_customCategories.ContainsKey(categoryName))
                {
                    MessageBox.Show("Такая категория уже существует!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _customCategories[categoryName] = new List<Tag>();
                InitializeCategoryCombo();
                CategoryCombo.SelectedItem = categoryName;

                MessageBox.Show($"Категория '{categoryName}' создана!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void DeleteCategory_Click(object sender, RoutedEventArgs e)
        {
            if (CategoryCombo.SelectedItem == null)
            {
                MessageBox.Show("Выберите категорию для удаления!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string selectedText = CategoryCombo.SelectedItem.ToString() ?? "";
            string selectedKey = _categoryNamesRu.FirstOrDefault(x => x.Value == selectedText).Key;

            if (string.IsNullOrEmpty(selectedKey))
                selectedKey = selectedText;

            if (_categoryMap.ContainsKey(selectedKey))
            {
                MessageBox.Show("Нельзя удалить стандартную категорию!\n\nМожно удалять только те категории, которые вы создали сами.",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_customCategories.TryGetValue(selectedKey, out var categoryTags) && categoryTags.Count > 0)
            {
                var result = MessageBox.Show(
                    $"В категории '{selectedKey}' есть {categoryTags.Count} тег(ов).\n\nПри удалении категории все теги будут потеряны!\n\nВы уверены?",
                    "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes) return;
            }
            else
            {
                var result = MessageBox.Show($"Удалить категорию '{selectedKey}'?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes) return;
            }

            _customCategories.Remove(selectedKey);

            CategoryCombo.Items.Clear();
            InitializeCategoryCombo();

            TagsList.Items.Clear();
            EditForm.Children.Clear();
            _selectedCategory = null;

            MessageBox.Show($"Категория '{selectedKey}' успешно удалена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        #region Вкладка 3: Связаться

        private void InitializeContactCards()
        {
            if (ContactCardsPanel == null) return;

            ContactCardsPanel.Children.Clear();

            ContactCardsPanel.Children.Add(CreateContactCard(
                "💬", "Discord",
                "Напишите в личные сообщения или на сервер",
                "@maze_m1",
                "#5865F2",
                "https://discord.com/invite/ZRvxvxnwB"
            ));

            ContactCardsPanel.Children.Add(CreateContactCard(
                "✈️", "Telegram",
                "Быстрый ответ в мессенджере",
                "@GREYCATNERO",
                "#0088CC",
                "https://t.me/GREYCATNERO"
            ));

            ContactCardsPanel.Children.Add(CreateContactCard(
                "📧", "Email",
                "Отправьте JSON файл на почту",
                "wiki@playgame-studio.ru",
                "#EA4335",
                "mailto:wiki@playgame-studio.ru?subject=Anime Tags JSON&body=Привет! Отправляю JSON файл..."
            ));
        }

        private Border CreateContactCard(string emoji, string title, string subtitle, string value, string colorHex, string url)
        {
            var color = (Color)ColorConverter.ConvertFromString(colorHex);
            var accentBrush = new SolidColorBrush(color);
            var accentBrushAlpha = new SolidColorBrush(Color.FromArgb(30, color.R, color.G, color.B));

            var card = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(51, 51, 51)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(20),
                Margin = new Thickness(0, 0, 0, 12),
                Cursor = Cursors.Hand,
                Tag = url
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var iconBorder = new Border
            {
                Width = 60,
                Height = 60,
                CornerRadius = new CornerRadius(30),
                Background = accentBrushAlpha,
                Child = new TextBlock
                {
                    Text = emoji,
                    FontSize = 32,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };

            var textStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(15, 0, 15, 0) };
            textStack.Children.Add(new TextBlock { Text = title, FontSize = 20, FontWeight = FontWeights.Bold, Foreground = Brushes.White });
            textStack.Children.Add(new TextBlock { Text = subtitle, FontSize = 13, Foreground = Brushes.Gray, Margin = new Thickness(0, 2, 0, 4) });
            textStack.Children.Add(new TextBlock { Text = value, FontSize = 15, Foreground = accentBrush, FontWeight = FontWeights.SemiBold });

            var arrowText = new TextBlock
            {
                Text = "→",
                FontSize = 32,
                Foreground = accentBrush,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            Grid.SetColumn(iconBorder, 0);
            Grid.SetColumn(textStack, 1);
            Grid.SetColumn(arrowText, 2);

            grid.Children.Add(iconBorder);
            grid.Children.Add(textStack);
            grid.Children.Add(arrowText);

            card.Child = grid;

            card.MouseEnter += (s, e) =>
            {
                card.BorderBrush = accentBrush;
                card.Background = new SolidColorBrush(Color.FromRgb(40, 40, 40));
            };
            card.MouseLeave += (s, e) =>
            {
                card.BorderBrush = new SolidColorBrush(Color.FromRgb(51, 51, 51));
                card.Background = new SolidColorBrush(Color.FromRgb(30, 30, 30));
            };

            card.MouseLeftButtonDown += (s, e) =>
            {
                try
                {
                    var link = card.Tag?.ToString();
                    if (!string.IsNullOrEmpty(link))
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = link,
                            UseShellExecute = true
                        });
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Не удалось открыть ссылку:\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };

            return card;
        }

        #endregion

        #region Вкладка 4: Предпросмотр сайта

        private void PreviewSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (PreviewSearchBox == null) return;
            _previewSearchQuery = PreviewSearchBox.Text.ToLower();
            RenderPreviewTags();
        }

        private void InitializePreviewFilters()
        {
            if (CategoryFiltersPanel == null) return;

            CategoryFiltersPanel.Children.Clear();
            CategoryFiltersPanel.Children.Add(CreateCategoryFilterButton("Все", "all"));

            foreach (var category in _categoryMap.Keys)
            {
                CategoryFiltersPanel.Children.Add(CreateCategoryFilterButton(
                    GetCategoryDisplayName(category), category));
            }

            foreach (var category in _customCategories.Keys)
            {
                CategoryFiltersPanel.Children.Add(CreateCategoryFilterButton(
                    category, category));
            }
        }

        private Button CreateCategoryFilterButton(string text, string categoryKey)
        {
            var button = new Button
            {
                Content = text,
                Tag = categoryKey,
                Margin = new Thickness(0, 0, 8, 8),
                Padding = new Thickness(12, 6, 12, 6),
                Background = categoryKey == "all"
                    ? new SolidColorBrush(Color.FromRgb(241, 196, 15))
                    : new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                Foreground = categoryKey == "all"
                    ? Brushes.Black
                    : Brushes.White,
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.FromRgb(51, 51, 51)),
                Cursor = Cursors.Hand,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold
            };

            button.Click += CategoryFilterButton_Click;
            return button;
        }

        private void CategoryFilterButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button) return;

            _previewSelectedCategory = button.Tag?.ToString() ?? "all";

            foreach (var child in CategoryFiltersPanel.Children)
            {
                if (child is Button btn)
                {
                    if (btn.Tag?.ToString() == _previewSelectedCategory)
                    {
                        btn.Background = new SolidColorBrush(Color.FromRgb(241, 196, 15));
                        btn.Foreground = Brushes.Black;
                    }
                    else
                    {
                        btn.Background = new SolidColorBrush(Color.FromRgb(30, 30, 30));
                        btn.Foreground = Brushes.White;
                    }
                }
            }

            RenderPreviewTags();
        }

        private void RenderPreviewTags()
        {
            if (_data == null || PreviewTagsPanel == null) return;
            if (CategoryFiltersPanel != null && CategoryFiltersPanel.Children.Count == 0)
                InitializePreviewFilters();

            PreviewTagsPanel.Children.Clear();

            var allTags = new List<(string Category, Tag Tag)>();
            foreach (var kvp in _categoryMap)
                foreach (var tag in kvp.Value(_data))
                    allTags.Add((kvp.Key, tag));

            foreach (var kvp in _customCategories)
                foreach (var tag in kvp.Value)
                    allTags.Add((kvp.Key, tag));

            var filteredTags = allTags.Where(x =>
            {
                var matchesSearch = string.IsNullOrEmpty(_previewSearchQuery) ||
                    (x.Tag.NameRu?.ToLower().Contains(_previewSearchQuery) ?? false) ||
                    (x.Tag.NameEn?.ToLower().Contains(_previewSearchQuery) ?? false);

                var matchesCategory = _previewSelectedCategory == "all" || x.Category == _previewSelectedCategory;

                return matchesSearch && matchesCategory;
            }).ToList();

            var groupedTags = filteredTags.GroupBy(x => x.Category).ToList();

            if (groupedTags.Count == 0)
            {
                PreviewTagsPanel.Children.Add(new TextBlock
                {
                    Text = "🔍 Ничего не найдено",
                    FontSize = 18,
                    Foreground = Brushes.Gray,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 40, 0, 0)
                });
                return;
            }

            foreach (var group in groupedTags)
            {
                PreviewTagsPanel.Children.Add(new TextBlock
                {
                    Text = $"📁 {GetCategoryDisplayName(group.Key)}",
                    FontSize = 20,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(241, 196, 15)),
                    Margin = new Thickness(0, 25, 0, 15)
                });

                var uniformGrid = new UniformGrid { Columns = 3, Margin = new Thickness(0, 0, 0, 10) };
                foreach (var (_, tag) in group)
                    uniformGrid.Children.Add(CreateTagPreviewCard(tag));
                PreviewTagsPanel.Children.Add(uniformGrid);
            }
        }

        private Border CreateTagPreviewCard(Tag tag)
        {
            var card = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(51, 51, 51)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(15),
                Margin = new Thickness(5),
                Cursor = Cursors.Hand
            };

            var stackPanel = new StackPanel();
            var headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var titleStack = new StackPanel();
            titleStack.Children.Add(new TextBlock
            {
                Text = tag.NameRu ?? tag.Slug,
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White
            });
            titleStack.Children.Add(new TextBlock
            {
                Text = tag.NameEn ?? "",
                FontSize = 12,
                Foreground = Brushes.Gray,
                FontStyle = FontStyles.Italic,
                Margin = new Thickness(0, 2, 0, 0)
            });

            var ratingText = new TextBlock
            {
                Text = tag.Meta?.AvgRating.HasValue == true ? $"{tag.Meta.AvgRating}★" : "—",
                FontSize = 12,
                Foreground = Brushes.Gray,
                VerticalAlignment = VerticalAlignment.Top
            };

            Grid.SetColumn(titleStack, 0);
            Grid.SetColumn(ratingText, 1);
            headerGrid.Children.Add(titleStack);
            headerGrid.Children.Add(ratingText);
            stackPanel.Children.Add(headerGrid);

            var description = !string.IsNullOrEmpty(tag.Wiki?.Overview) ? tag.Wiki.Overview : tag.ShortDesc;
            stackPanel.Children.Add(new TextBlock
            {
                Text = description ?? "Нет описания",
                FontSize = 13,
                Foreground = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 8, 0, 0)
            });

            card.Child = stackPanel;

            card.MouseEnter += (s, e) => card.BorderBrush = new SolidColorBrush(Color.FromRgb(241, 196, 15));
            card.MouseLeave += (s, e) => card.BorderBrush = new SolidColorBrush(Color.FromRgb(51, 51, 51));
            card.MouseLeftButtonDown += (s, e) => ShowTagDetailModal(tag);

            return card;
        }

        private void ShowTagDetailModal(Tag tag)
        {
            var modalWindow = new Window
            {
                Title = $"{tag.NameRu} — Предпросмотр",
                Width = 900,
                Height = 700,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                Background = new SolidColorBrush(Color.FromRgb(15, 15, 15))
            };

            var scrollViewer = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var mainPanel = new StackPanel { Margin = new Thickness(30) };

            var headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition());

            string firstLetter = !string.IsNullOrEmpty(tag.NameRu) ? tag.NameRu.Substring(0, 1).ToUpper() : "?";
            var iconBorder = new Border
            {
                Width = 80,
                Height = 80,
                CornerRadius = new CornerRadius(40),
                Background = new SolidColorBrush(Color.FromArgb(25, 241, 196, 15)),
                Child = new TextBlock
                {
                    Text = firstLetter,
                    FontSize = 36,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(241, 196, 15)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };

            var titleStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(20, 0, 0, 0) };
            titleStack.Children.Add(new TextBlock
            {
                Text = tag.NameRu ?? tag.Slug,
                FontSize = 32,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White
            });

            string tagCategory = "";
            foreach (var kvp in _categoryMap)
            {
                if (kvp.Value(_data).Contains(tag))
                {
                    tagCategory = GetCategoryDisplayName(kvp.Key);
                    break;
                }
            }

            titleStack.Children.Add(new TextBlock
            {
                Text = $"{tag.NameEn} | {tagCategory}",
                FontSize = 14,
                Foreground = Brushes.Gray,
                Margin = new Thickness(0, 5, 0, 0)
            });

            Grid.SetColumn(iconBorder, 0);
            Grid.SetColumn(titleStack, 1);
            headerGrid.Children.Add(iconBorder);
            headerGrid.Children.Add(titleStack);
            mainPanel.Children.Add(headerGrid);
            mainPanel.Children.Add(new Separator { Background = Brushes.Gray, Margin = new Thickness(0, 25, 0, 25) });

            if (!string.IsNullOrEmpty(tag.Wiki?.Overview))
            {
                mainPanel.Children.Add(CreateSectionHeader("📖 Описание"));
                mainPanel.Children.Add(new TextBlock
                {
                    Text = tag.Wiki.Overview,
                    FontSize = 15,
                    Foreground = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 20)
                });
            }

            var traitsGrid = new Grid();
            traitsGrid.ColumnDefinitions.Add(new ColumnDefinition());
            traitsGrid.ColumnDefinitions.Add(new ColumnDefinition());

            if (tag.Wiki?.KeyTraits != null && tag.Wiki.KeyTraits.Count > 0)
            {
                var traitsBox = CreateInfoBox("✨ Ключевые черты", tag.Wiki.KeyTraits, "•", new SolidColorBrush(Color.FromRgb(241, 196, 15)));
                Grid.SetColumn(traitsBox, 0);
                traitsGrid.Children.Add(traitsBox);
            }

            if (tag.Wiki?.ForWho != null && tag.Wiki.ForWho.Count > 0)
            {
                var whoBox = CreateInfoBox("👥 Для кого", tag.Wiki.ForWho, "✔", new SolidColorBrush(Color.FromRgb(46, 204, 113)));
                Grid.SetColumn(whoBox, 1);
                traitsGrid.Children.Add(whoBox);
            }

            if (traitsGrid.Children.Count > 0)
            {
                mainPanel.Children.Add(traitsGrid);
                mainPanel.Children.Add(new Separator { Background = Brushes.Gray, Margin = new Thickness(0, 25, 0, 25) });
            }

            if (tag.Wiki?.Examples != null && tag.Wiki.Examples.Count > 0)
            {
                mainPanel.Children.Add(CreateSectionHeader("🏆 Примеры"));
                foreach (var ex in tag.Wiki.Examples)
                {
                    var exBorder = new Border
                    {
                        Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                        BorderBrush = new SolidColorBrush(Color.FromRgb(51, 51, 51)),
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(8),
                        Padding = new Thickness(15),
                        Margin = new Thickness(0, 0, 0, 10)
                    };

                    var exGrid = new Grid();
                    exGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    exGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    exGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                    var rankText = new TextBlock
                    {
                        Text = $"#{tag.Wiki.Examples.IndexOf(ex) + 1}",
                        FontSize = 24,
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(Color.FromRgb(241, 196, 15)),
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(0, 0, 15, 0)
                    };

                    var titleText = new TextBlock
                    {
                        Text = ex.Title,
                        FontSize = 16,
                        FontWeight = FontWeights.Bold,
                        Foreground = Brushes.White,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(0, 0, 15, 0)
                    };

                    var reasonText = new TextBlock
                    {
                        Text = $"\"{ex.Reason}\"",
                        FontSize = 13,
                        Foreground = Brushes.Gray,
                        FontStyle = FontStyles.Italic,
                        TextWrapping = TextWrapping.Wrap,
                        VerticalAlignment = VerticalAlignment.Center
                    };

                    Grid.SetColumn(rankText, 0);
                    Grid.SetColumn(titleText, 1);
                    Grid.SetColumn(reasonText, 2);

                    exGrid.Children.Add(rankText);
                    exGrid.Children.Add(titleText);
                    exGrid.Children.Add(reasonText);

                    exBorder.Child = exGrid;
                    mainPanel.Children.Add(exBorder);
                }
                mainPanel.Children.Add(new Separator { Background = Brushes.Gray, Margin = new Thickness(0, 25, 0, 25) });
            }

            if (tag.Wiki?.Warnings != null && tag.Wiki.Warnings.Count > 0)
            {
                mainPanel.Children.Add(CreateSectionHeader("⚠️ Предупреждения"));
                var warningsPanel = new WrapPanel();
                foreach (var warn in tag.Wiki.Warnings)
                {
                    warningsPanel.Children.Add(new Border
                    {
                        Background = new SolidColorBrush(Color.FromArgb(20, 231, 76, 60)),
                        BorderBrush = new SolidColorBrush(Color.FromArgb(100, 231, 76, 60)),
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(15),
                        Padding = new Thickness(10, 5, 10, 5),
                        Margin = new Thickness(0, 0, 8, 8),
                        Child = new TextBlock
                        {
                            Text = warn,
                            Foreground = new SolidColorBrush(Color.FromRgb(231, 76, 60)),
                            FontSize = 13
                        }
                    });
                }
                mainPanel.Children.Add(warningsPanel);
            }

            if (tag.Meta != null)
            {
                var metaGrid = new Grid();
                metaGrid.ColumnDefinitions.Add(new ColumnDefinition());
                metaGrid.ColumnDefinitions.Add(new ColumnDefinition());
                metaGrid.ColumnDefinitions.Add(new ColumnDefinition());

                metaGrid.Children.Add(new TextBlock
                {
                    Text = $"⭐ Рейтинг: {tag.Meta.AvgRating?.ToString() ?? "—"}",
                    Foreground = Brushes.Gray,
                    FontSize = 13
                });

                var popularityText = new TextBlock
                {
                    Text = $"🔥 Популярность: #{tag.Meta.PopularityRank?.ToString() ?? "—"}",
                    Foreground = Brushes.Gray,
                    FontSize = 13,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                Grid.SetColumn(popularityText, 1);
                metaGrid.Children.Add(popularityText);

                var beginnerText = new TextBlock
                {
                    Text = tag.Meta.BeginnerFriendly == true ? "✅ Для новичков" : "❌ Сложно",
                    Foreground = tag.Meta.BeginnerFriendly == true
                        ? new SolidColorBrush(Color.FromRgb(46, 204, 113))
                        : Brushes.Gray,
                    FontSize = 13,
                    HorizontalAlignment = HorizontalAlignment.Right
                };
                Grid.SetColumn(beginnerText, 2);
                metaGrid.Children.Add(beginnerText);

                mainPanel.Children.Add(metaGrid);
            }

            var closeButton = new Button
            {
                Content = "✕ Закрыть",
                Width = 200,
                Height = 45,
                Margin = new Thickness(0, 30, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Center,
                Background = new SolidColorBrush(Color.FromRgb(231, 76, 60)),
                Foreground = Brushes.White,
                FontSize = 14,
                FontWeight = FontWeights.Bold
            };
            closeButton.Click += (s, e) => modalWindow.Close();
            mainPanel.Children.Add(closeButton);

            scrollViewer.Content = mainPanel;
            modalWindow.Content = scrollViewer;
            modalWindow.ShowDialog();
        }

        private TextBlock CreateSectionHeader(string text)
        {
            return new TextBlock
            {
                Text = text,
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 15)
            };
        }

        private Border CreateInfoBox(string title, List<string> items, string bullet, Brush accentColor)
        {
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(51, 51, 51)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(15),
                Margin = new Thickness(0, 0, 10, 0)
            };

            var stack = new StackPanel();
            stack.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 10)
            });

            foreach (var item in items)
            {
                stack.Children.Add(new TextBlock
                {
                    Text = $"{bullet} {item}",
                    Foreground = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                    FontSize = 13,
                    Margin = new Thickness(0, 3, 0, 0)
                });
            }

            border.Child = stack;
            return border;
        }

        private string GetCategoryDisplayName(string category)
        {
            return _categoryNamesRu.TryGetValue(category, out var name) ? name : category;
        }

        #endregion

        #region Управление видимостью кнопок

        private void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MainTabControl == null || BottomButtonsPanel == null) return;

            var selectedIndex = MainTabControl.SelectedIndex;

            if (selectedIndex == 0 || selectedIndex == 1)
            {
                BottomButtonsPanel.Visibility = Visibility.Visible;
            }
            else
            {
                BottomButtonsPanel.Visibility = Visibility.Collapsed;
            }
        }

        #endregion
    }

    public class TextInputDialog : Window
    {
        private readonly TextBox _textBox;
        public string Response { get; private set; } = "";

        public TextInputDialog(string prompt, string title)
        {
            Title = title;
            Width = 400;
            Height = 350;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Owner = Application.Current.MainWindow;
            Background = new SolidColorBrush(Color.FromRgb(18, 18, 18));

            var grid = new Grid { Margin = new Thickness(10) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var label = new TextBlock { Text = prompt, Margin = new Thickness(0, 0, 0, 10), TextWrapping = TextWrapping.Wrap, Foreground = Brushes.White };
            _textBox = new TextBox { Margin = new Thickness(0, 0, 0, 10), Background = new SolidColorBrush(Color.FromRgb(50, 50, 50)), Foreground = Brushes.White };

            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var okButton = new Button { Content = "OK", Width = 75, Margin = new Thickness(5, 0, 0, 0) };
            var cancelButton = new Button { Content = "Отмена", Width = 75, Margin = new Thickness(5, 0, 0, 0) };

            okButton.Click += (s, e) => { Response = _textBox.Text; DialogResult = true; Close(); };
            cancelButton.Click += (s, e) => { DialogResult = false; Close(); };

            buttonPanel.Children.Add(cancelButton);
            buttonPanel.Children.Add(okButton);

            Grid.SetRow(label, 0);
            Grid.SetRow(_textBox, 1);
            Grid.SetRow(buttonPanel, 2);

            grid.Children.Add(label);
            grid.Children.Add(_textBox);
            grid.Children.Add(buttonPanel);

            Content = grid;
        }
    }
}