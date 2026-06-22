using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using Hercules.UI.ViewModels.Settings;

namespace Hercules.UI.Elements.Settings.Pages
{
    public partial class BehaviourPage
    {
        public BehaviourPage()
        {
            InitializeComponent();
            DataContext = new BehaviourViewModel();
        }
    }
}
