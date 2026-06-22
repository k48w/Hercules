using Hercules.Integrations;
using Hercules.UI.Elements.Base;
using Hercules.UI.ViewModels.ContextMenu;

namespace Hercules.UI.Elements.ContextMenu
{
    public partial class BetterBloxDataCenterConsole
    {
        public BetterBloxDataCenterConsole()
        {
            InitializeComponent();
            var vm = new BetterBloxDataCenterConsoleViewModel();
            DataContext = vm;
        }
    }
}
