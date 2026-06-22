using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Hercules.Enums;
using Hercules.UI.Elements.Base;

namespace Hercules.UI.Elements.Dialogs
{
    /// <summary>
    /// Dialog for previewing cursor types before applying them
    /// </summary>
    public partial class CursorPreviewDialog : WpfUiWindow
    {
        public Hercules.Enums.CursorType? SelectedCursor { get; private set; }

        public CursorPreviewDialog()
        {
            InitializeComponent();
            LoadCursorPreviews();
        }

        private void LoadCursorPreviews()
        {
            var cursors = new[]
            {
                Hercules.Enums.CursorType.Default,
                Hercules.Enums.CursorType.FPSCursor,
                Hercules.Enums.CursorType.CleanCursor,
                Hercules.Enums.CursorType.DotCursor,
                Hercules.Enums.CursorType.StoofsCursor,
                Hercules.Enums.CursorType.From2006,
                Hercules.Enums.CursorType.From2013,
                Hercules.Enums.CursorType.WhiteDotCursor,
                Hercules.Enums.CursorType.VerySmallWhiteDot
            };

            foreach (var cursor in cursors)
            {
                var previewItem = CreateCursorPreviewItem(cursor);
                CursorStackPanel.Children.Add(previewItem);
            }
        }

        private FrameworkElement CreateCursorPreviewItem(Hercules.Enums.CursorType cursor)
        {
            var border = new System.Windows.Controls.Border
            {
                BorderBrush = new SolidColorBrush(Colors.Gray),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(5),
                Padding = new Thickness(10),
                Background = new SolidColorBrush(Colors.Transparent),
                Cursor = System.Windows.Input.Cursors.Hand
            };

            var stackPanel = new System.Windows.Controls.StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal
            };

            // Load cursor image for preview
            var image = new System.Windows.Controls.Image
            {
                Width = 32,
                Height = 32,
                Margin = new Thickness(0, 0, 10, 0)
            };

            try
            {
                var imagePath = GetCursorImagePath(cursor);
                if (!string.IsNullOrEmpty(imagePath))
                {
                    var uri = new Uri($"pack://application:,,,/Resources/Mods/{imagePath}");
                    image.Source = new BitmapImage(uri);
                }
            }
            catch
            {
                // Use default image if cursor image can't be loaded
                image.Source = null;
            }

            var nameLabel = new System.Windows.Controls.TextBlock
            {
                Text = GetCursorDisplayName(cursor),
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 14
            };

            stackPanel.Children.Add(image);
            stackPanel.Children.Add(nameLabel);
            border.Child = stackPanel;

            border.MouseLeftButtonUp += (s, e) =>
            {
                SelectedCursor = cursor;
                DialogResult = true;
                Close();
            };

            border.MouseEnter += (s, e) =>
            {
                border.Background = new SolidColorBrush(Color.FromArgb(50, 100, 149, 237));
            };

            border.MouseLeave += (s, e) =>
            {
                border.Background = new SolidColorBrush(Colors.Transparent);
            };

            return border;
        }

        private string GetCursorImagePath(Hercules.Enums.CursorType cursor)
        {
            return cursor switch
            {
                Hercules.Enums.CursorType.FPSCursor => "Cursor/FPSCursor/ArrowCursor.png",
                Hercules.Enums.CursorType.CleanCursor => "Cursor/CleanCursor/ArrowCursor.png",
                Hercules.Enums.CursorType.DotCursor => "Cursor/DotCursor/ArrowCursor.png",
                Hercules.Enums.CursorType.StoofsCursor => "Cursor/StoofsCursor/ArrowCursor.png",
                Hercules.Enums.CursorType.From2006 => "Cursor/From2006/ArrowCursor.png",
                Hercules.Enums.CursorType.From2013 => "Cursor/From2013/ArrowCursor.png",
                Hercules.Enums.CursorType.WhiteDotCursor => "Cursor/WhiteDotCursor/ArrowCursor.png",
                Hercules.Enums.CursorType.VerySmallWhiteDot => "Cursor/VerySmallWhiteDot/ArrowCursor.png",
                _ => string.Empty
            };
        }

        private string GetCursorDisplayName(Hercules.Enums.CursorType cursor)
        {
            return cursor switch
            {
                Hercules.Enums.CursorType.Default => "Default",
                Hercules.Enums.CursorType.FPSCursor => "FPS Cursor (V1)",
                Hercules.Enums.CursorType.CleanCursor => "Clean Cursor",
                Hercules.Enums.CursorType.DotCursor => "Dot Cursor",
                Hercules.Enums.CursorType.StoofsCursor => "Stoofs Cursor",
                Hercules.Enums.CursorType.From2006 => "2006 Legacy Cursor",
                Hercules.Enums.CursorType.From2013 => "2013 Legacy Cursor",
                Hercules.Enums.CursorType.WhiteDotCursor => "White Dot Cursor",
                Hercules.Enums.CursorType.VerySmallWhiteDot => "Very Small White Dot",
                _ => cursor.ToString()
            };
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}