using System.Windows;
using Microsoft.Win32;
using Hercules.Integrations;
using Hercules.UI.Elements.Base;

namespace Hercules.UI.Elements.Dialogs
{
    public partial class ImportExportDialog : WpfUiWindow
    {
        public ImportExportDialog()
        {
            InitializeComponent();
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Hercules Config (*.hercules)|*.hercules|JSON Files (*.json)|*.json",
                DefaultExt = ".hercules",
                FileName = $"HerculesConfig_{DateTime.Now:yyyyMMdd_HHmmss}.hercules"
            };

            if (dialog.ShowDialog() != true)
                return;

            try
            {
                ConfigExporter.ExportToFile(dialog.FileName);
                StatusText.Text = $"Configuration exported successfully to:\n{dialog.FileName}";
                StatusText.Foreground = System.Windows.Media.Brushes.LimeGreen;
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Export failed: {ex.Message}";
                StatusText.Foreground = System.Windows.Media.Brushes.OrangeRed;
            }
        }

        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Hercules Config (*.hercules)|*.hercules|JSON Files (*.json)|*.json",
                DefaultExt = ".hercules"
            };

            if (dialog.ShowDialog() != true)
                return;

            try
            {
                var config = ConfigExporter.ImportFromFile(dialog.FileName);
                if (config is null)
                {
                    StatusText.Text = "Failed to parse configuration file.";
                    StatusText.Foreground = System.Windows.Media.Brushes.OrangeRed;
                    return;
                }

                int itemCount = ConfigExporter.CountExportedItems(config);
                string summary = $"FastFlags: {config.FastFlags.Count} keys\n";
                if (!string.IsNullOrEmpty(config.ClientSettings)) summary += "• ClientSettings\n";
                if (!string.IsNullOrEmpty(config.BootstrapperSettings)) summary += "• Bootstrapper Settings\n";
                if (!string.IsNullOrEmpty(config.StateSettings)) summary += "• State Settings\n";
                if (!string.IsNullOrEmpty(config.RobloxStateSettings)) summary += "• Roblox State\n";
                if (!string.IsNullOrEmpty(config.ThemeSettings)) summary += "• Theme Presets\n";
                if (!string.IsNullOrEmpty(config.GlobalBasicSettings)) summary += "• Global Basic Settings\n";

                var result = Frontend.ShowMessageBox(
                    $"Import configuration from:\n{dialog.FileName}\n\nFound {itemCount} configuration sections:\n{summary}\nApply this configuration?",
                    MessageBoxImage.Question,
                    MessageBoxButton.YesNo
                );

                if (result != MessageBoxResult.Yes)
                    return;

                ConfigExporter.ApplyConfig(config);
                StatusText.Text = "Configuration imported and applied successfully!";
                StatusText.Foreground = System.Windows.Media.Brushes.LimeGreen;
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Import failed: {ex.Message}";
                StatusText.Foreground = System.Windows.Media.Brushes.OrangeRed;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
