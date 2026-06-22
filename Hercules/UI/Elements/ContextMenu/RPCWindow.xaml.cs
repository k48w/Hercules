using System.Windows;
using Hercules.UI.ViewModels;
using Hercules.UI.ViewModels.ContextMenu;

namespace Hercules.UI.Elements.ContextMenu
{
    public partial class RPCWindow
    {
        public RPCWindow()
        {
            InitializeComponent();
            DataContext = new RPCCustomizerViewModel();
        }
    }
}
