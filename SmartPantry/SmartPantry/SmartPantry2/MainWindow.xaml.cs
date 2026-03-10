// ============================================================
// Datei:   MainWindow.xaml.cs
// Schicht: UI / Einstiegspunkt
//
// ZWECK:
//   Code-Behind des Hauptfensters.
//   Verbindet das XAML (MainWindow.xaml) mit dem MainViewModel
//   als DataContext → alle XAML-Bindings greifen auf MainViewModel zu.
//
// ROTER FADEN:
//   App.xaml.cs startet → new MainWindow()
//     → InitializeComponent() → XAML parsen und Steuerelemente erstellen
//     → DatabaseTester.TestConnection() → Verbindung prüfen
//     → DataContext = new MainViewModel()
//       → MainViewModel erstellt alle Sub-ViewModels
//       → CurrentViewModel = LoginVM
//       → ContentControl in MainWindow zeigt LoginView
//
// USER USECASE:
//   App doppelklicken → MainWindow erscheint mit Login-Seite
//
// QUELLEN:
//   WPF Code-Behind (Microsoft Learn):
//   https://learn.microsoft.com/dotnet/desktop/wpf/xaml/
//
//   DataContext in WPF:
//   https://learn.microsoft.com/dotnet/desktop/wpf/data/
//
//   InitializeComponent() (auto-generierter Code):
//   https://learn.microsoft.com/dotnet/desktop/wpf/xaml/xaml-overview
// ============================================================

using Smartpantry.Helpers;
using Smartpantry.ViewModels;
using System.Windows;

namespace SmartPantry2
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            // InitializeComponent(): von Visual Studio auto-generierte Methode.
            // Parst MainWindow.xaml und erstellt alle Steuerelemente im RAM.
            // MUSS als erstes aufgerufen werden!
            // Quelle: https://learn.microsoft.com/dotnet/desktop/wpf/xaml/xaml-overview
            InitializeComponent();

            // Datenbankverbindung testen bevor User etwas sieht
            // → bei Fehler: MessageBox mit Fehlermeldung
            // Quelle: DatabaseTester.cs
            DatabaseTester.TestConnection();

            // DataContext setzen: verbindet ALLE XAML-Bindings mit MainViewModel
            // Alle "{Binding ...}" in MainWindow.xaml und allen Views
            // suchen ihren Wert ab jetzt im MainViewModel (oder dessen Sub-VMs)
            // Quelle: https://learn.microsoft.com/dotnet/desktop/wpf/data/
            DataContext = new MainViewModel();
        }
    }
}