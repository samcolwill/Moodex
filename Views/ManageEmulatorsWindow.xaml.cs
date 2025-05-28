using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using SamsGameLauncher.Models;
using SamsGameLauncher.ViewModels;

namespace SamsGameLauncher.Views
{
    /// <summary>
    /// Interaction logic for ManageEmulatorsWindow.xaml
    /// </summary>
    public partial class ManageEmulatorsWindow : Window
    {
        public ManageEmulatorsWindow(ManageEmulatorsWindowViewModel vm)
        {
            InitializeComponent();

            // let DI give us the ViewModel
            DataContext = vm;
        }

        private void ListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is System.Windows.Controls.ListView list
             && list.SelectedItem is EmulatorInfo emu
             && DataContext is ManageEmulatorsWindowViewModel vm
             && vm.OpenEmulatorCommand.CanExecute(emu))
            {
                vm.OpenEmulatorCommand.Execute(emu);
            }
        }
    }
}
