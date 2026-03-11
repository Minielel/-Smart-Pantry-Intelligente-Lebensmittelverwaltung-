// ------------------------------------------------------------
// Datei: MainWindow.xaml.cs
//
// Beschreibung:
// Diese Datei gehört zum Grundaufbau des Projekts. Sie hilft dabei, die Anwendung zu starten, Ressourcen zu laden oder die Hauptansicht zu steuern.
//
// Hinweis fuer die Vorstellung:
// Wenn man diese Datei in der Schule erklaeren moechte, kann man sagen,
// dass sie einen bestimmten Baustein der App uebernimmt und dadurch hilft,
// die Anwendung klar zu strukturieren.
// ------------------------------------------------------------
using Smartpantry.Helpers;
using Smartpantry.ViewModels;
using System.Windows;

namespace SmartPantry2
{



    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();



            DatabaseTester.TestConnection();

            DataContext = new MainViewModel();
        }
    }
}
