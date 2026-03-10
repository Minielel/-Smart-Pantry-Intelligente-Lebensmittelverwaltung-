// ============================================================
// Datei:   DashboardViewModel.cs
// Schicht: ViewModel / Dashboard
//
// ZWECK:
//   Versorgt DashboardView.xaml mit Statistiken.
//   Aktualisiert automatisch alle 30 Sekunden per DispatcherTimer.
//   ExpiryAlertLevel steuert die Warnfarbe der Ablauf-Kachel.
//
// ROTER FADEN:
//   DashboardView.xaml ←→ DashboardViewModel ←→ DashboardService ←→ DB
//
//   ExpiryAlertLevel → DataTrigger in DashboardView.xaml:
//   "None"    → normale Hintergrundfarbe (Surface2Brush)
//   "Soon"    → gelb (#FFF3D57A) – Items laufen bald ab
//   "Expired" → rot (#FFF09A9A)  – Items bereits abgelaufen
//
// USER USECASE:
//   App-Start → Refresh() → Kacheln mit Zahlen füllen
//   Alle 30 Sek. → automatisch aktualisieren
//   "Aktualisieren"-Button → manuell RefreshCommand ausführen
//
// QUELLEN:
//   DispatcherTimer (WPF UI-Thread-Timer):
//   https://learn.microsoft.com/dotnet/api/system.windows.threading.dispatchertimer
//
//   C# Value Tuples (Dekonstruktion):
//   https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/value-tuples
//
//   C# switch expression:
//   https://learn.microsoft.com/dotnet/csharp/language-reference/operators/switch-expression
// ============================================================

using Smartpantry.Helpers;
using SmartPantry2.Services;
using System;
using System.Windows.Threading;

namespace Smartpantry.ViewModels
{
    public class DashboardViewModel : BaseViewModel
    {
        // DashboardService: holt Statistiken aus der DB
        private readonly DashboardService _dashboardService = new DashboardService();

        // DispatcherTimer: läuft im UI-Thread (kein Cross-Thread-Problem)
        // → sicher für Property-Updates die WPF-Bindings triggern
        // Quelle: https://learn.microsoft.com/dotnet/api/system.windows.threading.dispatchertimer
        private readonly DispatcherTimer _timer;

        // Gesamtanzahl der FoodItems → Kachel "Items" in DashboardView
        private int _totalFoodItems;
        public int TotalFoodItems { get => _totalFoodItems; set => SetProperty(ref _totalFoodItems, value); }

        // Items die bald ablaufen → Kachel "Läuft bald ab" / "Expiring soon"
        private int _expiringSoon;
        public int ExpiringSoon { get => _expiringSoon; set => SetProperty(ref _expiringSoon, value); }

        // Items die bereits abgelaufen sind (für ExpiryAlertLevel-Logik)
        private int _expired;
        public int Expired { get => _expired; set => SetProperty(ref _expired, value); }

        // Steuert die Warnfarbe der Ablauf-Kachel in DashboardView.xaml
        // per DataTrigger: "None" | "Soon" | "Expired"
        // Quelle DataTrigger: https://learn.microsoft.com/dotnet/desktop/wpf/data/data-templating-overview
        private string _expiryAlertLevel = "None";
        public string ExpiryAlertLevel { get => _expiryAlertLevel; set => SetProperty(ref _expiryAlertLevel, value); }

        // Anzahl der Rezepte → Kachel "Ideen" / "Ideas"
        private int _totalRecipes;
        public int TotalRecipes { get => _totalRecipes; set => SetProperty(ref _totalRecipes, value); }

        // Fehler- oder Statusmeldung für die View
        private string _statusMessage = "";
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

        // Command für "Aktualisieren"-Button in DashboardView
        public RelayCommand RefreshCommand { get; }

        public DashboardViewModel()
        {
            RefreshCommand = new RelayCommand(Refresh);

            // Bei Login/Logout → sofort neu laden
            UserSession.CurrentUserChanged += Refresh;

            // DispatcherTimer: alle 30 Sekunden automatisch aktualisieren
            // Interval: TimeSpan.FromSeconds(30) = 30 Sekunden
            // Quelle: https://learn.microsoft.com/dotnet/api/system.windows.threading.dispatchertimer
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(30)
            };
            // Lambda als Tick-Handler: ignoriert Sender und EventArgs (_)
            _timer.Tick += (_, __) => Refresh();
            // Timer starten → läuft bis App geschlossen wird
            _timer.Start();

            // Erste Ladung beim Start
            Refresh();
        }

        // --------------------------------------------------------
        // Refresh
        //
        // FUNKTION:
        //   Lädt alle Dashboard-Statistiken neu aus der Datenbank.
        //   Setzt danach ExpiryAlertLevel für die Warnfarbe.
        //
        // AUFGERUFEN VON:
        //   Konstruktor (einmal beim Start)
        //   DispatcherTimer.Tick (alle 30 Sekunden)
        //   UserSession.CurrentUserChanged (bei Login/Logout)
        //   RefreshCommand (manueller Klick auf "Aktualisieren")
        // --------------------------------------------------------
        public void Refresh()
        {
            try
            {
                StatusMessage = "";

                // Aktuell eingeloggte UserId (null wenn nicht eingeloggt)
                var userId = UserSession.CurrentUserId;

                // Tupel-Dekonstruktion: alle 4 Werte auf einmal auslesen
                // Quelle: https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/value-tuples
                var (total, expSoon, expired, recipes) = _dashboardService.GetStats(userId);

                // Properties setzen → PropertyChanged → WPF aktualisiert Kacheln
                TotalFoodItems = total;
                ExpiringSoon = expSoon;
                Expired = expired;
                TotalRecipes = recipes;

                // Warnlevel berechnen: rot hat Vorrang vor gelb
                if (Expired > 0) ExpiryAlertLevel = "Expired";     // rot
                else if (ExpiringSoon > 0) ExpiryAlertLevel = "Soon"; // gelb
                else ExpiryAlertLevel = "None";                        // normal
            }
            catch (Exception ex)
            {
                // Fehler abfangen → Meldung anzeigen statt abstürzen
                StatusMessage = "Fehler beim Laden der Stats: " + ex.Message;
            }
        }
    }
}