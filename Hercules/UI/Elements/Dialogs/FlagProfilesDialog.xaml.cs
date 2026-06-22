using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Hercules.Resources;

namespace Hercules.UI.Elements.Dialogs
{
    /// <summary>
    /// Interaction logic for FlagProfilesDialog.xaml
    /// </summary>
    public partial class FlagProfilesDialog
    {
        public MessageBoxResult Result { get; private set; } = MessageBoxResult.Cancel;

        public FlagProfilesDialog()
        {
            InitializeComponent();
            LoadBackups();
        }

        private void LoadBackups()
        {
            LoadBackup.Items.Clear();

            var backupsDirectory = Path.Combine(Paths.Base, Paths.SavedBackups);

            try
            {
                if (!Directory.Exists(backupsDirectory))
                {
                    Directory.CreateDirectory(backupsDirectory);
                }

                foreach (var profilePath in Directory.GetFiles(backupsDirectory))
                {
                    var profileName = Path.GetFileName(profilePath);
                    if (!string.IsNullOrWhiteSpace(profileName))
                    {
                        LoadBackup.Items.Add(profileName);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading backups: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.OK;
            this.Close();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (LoadBackup.SelectedItem is string selectedProfileName && !string.IsNullOrWhiteSpace(selectedProfileName))
            {
                App.FastFlags.DeleteBackup(selectedProfileName);
                LoadBackups();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
        }
    }
}
