using SmartPantry2.Data;
using Smartpantry.Helpers;
using Smartpantry.Models;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Windows;
//Der Typ- oder Namespacename "Services" ist im Namespace "Smartpantry" nicht vorhanden. (Möglicherweise fehlt ein Assemblyverweis.)
//Der Typ- oder Namespacename "ViewModels" ist im Namespace "Smartpantry" nicht vorhanden. (Möglicherweise fehlt ein Assemblyverweis.)
//Der Typ- oder Namespacename "View" ist im Namespace "Smartpantry" nicht vorhanden. (Möglicherweise fehlt ein Assemblyverweis.)
// |
// v
//using Smartpantry.Services;
//using Smartpantry.ViewModels;
//using Smartpantry.View;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SmartPantry2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DatabaseTester.TestConnection();
        }
    }
}