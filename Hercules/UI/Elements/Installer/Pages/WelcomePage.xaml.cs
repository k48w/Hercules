using System.Windows;
using System.Windows.Navigation;
using Hercules.UI.ViewModels.Installer;

namespace Hercules.UI.Elements.Installer.Pages
{
    /// <summary>
    /// Interaction logic for WelcomePage.xaml
    /// </summary>
    public partial class WelcomePage
    {
        private readonly WelcomeViewModel _viewModel = new();

        public WelcomePage()
        {
            InitializeComponent();
            DataContext = _viewModel;
        }

        private void UiPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is MainWindow window)
                window.SetNextButtonText(Strings.Common_Navigation_Next);
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Utilities.OpenWebsite(e.Uri.AbsoluteUri);
            e.Handled = true;
        }
        private void DonateButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Utilities.OpenWebsite($"https://github.com/{App.ProjectRepository}");
        }
        private void ContributorsButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Utilities.OpenWebsite($"https://github.com/{App.ProjectRepository}");
        }
    }
}
