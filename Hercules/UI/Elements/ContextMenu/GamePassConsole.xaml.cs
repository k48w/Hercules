using Hercules.Integrations;
using Hercules.UI.Elements.Base;
using Hercules.UI.ViewModels.ContextMenu;

namespace Hercules.UI.Elements.ContextMenu
{
    public partial class GamePassConsole
    {
        public GamePassConsole(long userId)
        {
            InitializeComponent();
            var vm = new GamePassConsoleViewModel();
            DataContext = vm;
            vm.LoadGamePassesCommand.Execute(userId);
        }
    }
}
