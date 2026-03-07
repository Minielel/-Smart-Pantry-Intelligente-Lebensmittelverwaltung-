// ------------------------------------------------------------
// Datei: DashboardViewModel.cs
//
// Beschreibung:
// Diese Datei gehört zur Logik der Benutzeroberfläche. In einem ViewModel werden Eingaben verarbeitet, Daten vorbereitet und Befehle für Buttons bereitgestellt.
//
// Hinweis fuer die Vorstellung:
// Wenn man diese Datei in der Schule erklaeren moechte, kann man sagen,
// dass sie einen bestimmten Baustein der App uebernimmt und dadurch hilft,
// die Anwendung klar zu strukturieren.
// ------------------------------------------------------------
using Smartpantry.Helpers;
using SmartPantry2.Services;
using System;
using System.Windows.Threading;

namespace Smartpantry.ViewModels
{
    public class DashboardViewModel : BaseViewModel
    {
        private readonly DashboardService _dashboardService = new DashboardService();
        private readonly DispatcherTimer _timer;

        private int _totalFoodItems;
        public int TotalFoodItems { get => _totalFoodItems; set => SetProperty(ref _totalFoodItems, value); }

        private int _expiringSoon;
        public int ExpiringSoon { get => _expiringSoon; set => SetProperty(ref _expiringSoon, value); }

        private int _expired;
        public int Expired { get => _expired; set => SetProperty(ref _expired, value); }


        private string _expiryAlertLevel = "None";
        public string ExpiryAlertLevel { get => _expiryAlertLevel; set => SetProperty(ref _expiryAlertLevel, value); }

        private int _totalRecipes;
        public int TotalRecipes { get => _totalRecipes; set => SetProperty(ref _totalRecipes, value); }

        private string _statusMessage = "";
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

        public RelayCommand RefreshCommand { get; }

        public DashboardViewModel()
        {
            RefreshCommand = new RelayCommand(Refresh);
            UserSession.CurrentUserChanged += Refresh;


            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(30)
            };
            _timer.Tick += (_, __) => Refresh();
            _timer.Start();

            Refresh();
        }

        public void Refresh()
        {
            try
            {
                StatusMessage = "";
                var userId = UserSession.CurrentUserId;
                var (total, expSoon, expired, recipes) = _dashboardService.GetStats(userId);

                TotalFoodItems = total;
                ExpiringSoon = expSoon;
                Expired = expired;
                TotalRecipes = recipes;


                if (Expired > 0) ExpiryAlertLevel = "Expired";
                else if (ExpiringSoon > 0) ExpiryAlertLevel = "Soon";
                else ExpiryAlertLevel = "None";
            }
            catch (Exception ex)
            {
                StatusMessage = "Fehler beim Laden der Stats: " + ex.Message;
            }
        }
    }
}
