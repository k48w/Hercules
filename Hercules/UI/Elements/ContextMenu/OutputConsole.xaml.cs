using Hercules.Integrations;
using Hercules.UI.ViewModels.ContextMenu;

namespace Hercules.UI.Elements.ContextMenu
{
    public partial class OutputConsole
    {
        public OutputConsole(ActivityWatcher watcher)
        {
            var viewModel = new OutputConsoleViewModel(watcher);

            viewModel.RequestCloseEvent += (_, _) => Close();

            DataContext = viewModel;
            InitializeComponent();
        }
    }
}
