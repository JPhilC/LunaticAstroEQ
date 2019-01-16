using Lunatic.TelescopeController.ViewModel;
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

namespace Lunatic.TelescopeController.Controls
{
   /// <summary>
   /// Interaction logic for GotoWindow.xaml
   /// </summary>
   public partial class GotoWindow : Window
   {
      MainViewModel _ViewModel;
      public GotoWindow(MainViewModel viewModel)
      {
         InitializeComponent();
         _ViewModel = viewModel;
         DataContext = viewModel;
      }

      protected override void OnClosed(EventArgs e)
      {
         base.OnClosed(e);
         // TODO: Reinstate _ViewModel.OnGotoWindowClosed();
      }

   }
}
