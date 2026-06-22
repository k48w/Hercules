using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Hercules.Integrations;
using Hercules.Models;
using Hercules.UI.Elements.Base;

namespace Hercules.UI.Elements.Dialogs
{
    /// <summary>
    /// Dialog for selecting preset FFlag values and applying preset themes
    /// </summary>
    public partial class FFlagPresetsDialog : WpfUiWindow
    {
        public string? SelectedValue { get; private set; }
        public bool ThemeApplied { get; private set; }

        private static readonly Dictionary<string, string[]> PresetCategories = new()
        {
            { "Boolean", new[] { "True", "False" } },
            { "Basic Numbers", new[] { "0", "1", "10", "100", "1000" } },
            { "Large Numbers", new[] { "10000", "100000", "1000000", "2147483647" } },
            { "Percentages", new[] { "0", "25", "50", "75", "100" } },
            { "FPS Values", new[] { "30", "60", "120", "144", "240", "360" } },
            { "Quality Levels", new[] { "0", "1", "2", "3", "4", "5", "10", "21" } },
            { "Special Values", new[] { "-1", "null", "\"\"" } },
            { "Memory Values", new[] { "1024", "2048", "4096", "8192", "16384" } }
        };

        public FFlagPresetsDialog()
        {
            InitializeComponent();
            LoadPresetCategories();
            LoadThemes();
        }

        private void LoadPresetCategories()
        {
            foreach (var category in PresetCategories)
            {
                var expander = new Expander
                {
                    Header = category.Key,
                    Margin = new Thickness(0, 5, 0, 5),
                    IsExpanded = category.Key == "Boolean"
                };

                var stackPanel = new StackPanel();

                foreach (var value in category.Value)
                {
                    var button = new Button
                    {
                        Content = value,
                        Margin = new Thickness(2, 2, 2, 2),
                        Padding = new Thickness(8, 4, 8, 4),
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        Background = new SolidColorBrush(Colors.Transparent),
                        BorderBrush = new SolidColorBrush(Colors.Gray),
                        BorderThickness = new Thickness(1, 1, 1, 1)
                    };

                    button.Click += (s, e) =>
                    {
                        SelectedValue = value;
                        DialogResult = true;
                        Close();
                    };

                    button.MouseEnter += (s, e) =>
                    {
                        button.Background = new SolidColorBrush(
                            Color.FromArgb(50, 100, 149, 237));
                    };

                    button.MouseLeave += (s, e) =>
                    {
                        button.Background = new SolidColorBrush(Colors.Transparent);
                    };

                    stackPanel.Children.Add(button);
                }

                expander.Content = stackPanel;
                PresetStackPanel.Children.Add(expander);
            }
        }

        private void LoadThemes()
        {
            var themes = FlagPresetThemesProvider.GetAllThemes();

            foreach (var theme in themes)
            {
                var card = CreateThemeCard(theme);
                ThemePanel.Children.Add(card);
            }
        }

        private Border CreateThemeCard(FlagPresetTheme theme)
        {
            var card = new Border
            {
                Width = 280,
                Margin = new Thickness(4),
                Padding = new Thickness(12),
                CornerRadius = new CornerRadius(8),
                Background = new SolidColorBrush(Color.FromArgb(20, 128, 128, 128)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(40, 200, 200, 200)),
                BorderThickness = new Thickness(1)
            };

            var innerStack = new StackPanel();

            var headerPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 4) };

            headerPanel.Children.Add(new TextBlock
            {
                Text = theme.Icon,
                FontSize = 24,
                Margin = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center
            });

            headerPanel.Children.Add(new TextBlock
            {
                Text = theme.Name,
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Colors.White),
                VerticalAlignment = VerticalAlignment.Center
            });

            innerStack.Children.Add(headerPanel);

            innerStack.Children.Add(new TextBlock
            {
                Text = theme.Description,
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(180, 180, 180)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 6)
            });

            var categoryBadge = new Border
            {
                Padding = new Thickness(8, 2, 8, 2),
                CornerRadius = new CornerRadius(4),
                Background = GetCategoryColor(theme.Category),
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 0, 0, 6),
                Child = new TextBlock
                {
                    Text = theme.Category,
                    FontSize = 11,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Colors.White)
                }
            };
            innerStack.Children.Add(categoryBadge);

            if (theme.Tags.Count > 0)
            {
                var tagsPanel = new WrapPanel { Margin = new Thickness(0, 0, 0, 8) };
                foreach (var tag in theme.Tags)
                {
                    tagsPanel.Children.Add(new Border
                    {
                        Padding = new Thickness(6, 1, 6, 1),
                        Margin = new Thickness(0, 2, 4, 2),
                        CornerRadius = new CornerRadius(3),
                        Background = new SolidColorBrush(Color.FromArgb(30, 200, 200, 200)),
                        Child = new TextBlock
                        {
                            Text = tag,
                            FontSize = 10,
                            Foreground = new SolidColorBrush(Color.FromRgb(200, 200, 200))
                        }
                    });
                }
                innerStack.Children.Add(tagsPanel);
            }

            var applyButton = new Button
            {
                Content = "Apply",
                Margin = new Thickness(0, 4, 0, 0),
                Padding = new Thickness(8, 4, 8, 4),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Background = new SolidColorBrush(Color.FromRgb(45, 110, 200)),
                Foreground = new SolidColorBrush(Colors.White),
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            applyButton.Click += (s, e) =>
            {
                FlagPresetThemesProvider.ApplyTheme(theme);
                ThemeApplied = true;
                DialogResult = true;
                Close();
            };
            innerStack.Children.Add(applyButton);

            card.Child = innerStack;
            return card;
        }

        private static SolidColorBrush GetCategoryColor(string category)
        {
            return category switch
            {
                "Performance" => new SolidColorBrush(Color.FromRgb(0, 120, 212)),
                "Visual" => new SolidColorBrush(Color.FromRgb(140, 70, 200)),
                "Network" => new SolidColorBrush(Color.FromRgb(0, 150, 100)),
                "Privacy" => new SolidColorBrush(Color.FromRgb(200, 100, 30)),
                "Competitive" => new SolidColorBrush(Color.FromRgb(200, 50, 50)),
                "LowEnd" => new SolidColorBrush(Color.FromRgb(120, 120, 120)),
                _ => new SolidColorBrush(Color.FromRgb(100, 100, 100))
            };
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}