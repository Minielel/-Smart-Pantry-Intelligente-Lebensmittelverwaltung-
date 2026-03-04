using Smartpantry.Helpers;
using SmartPantry2.Services;
using System;

namespace Smartpantry.ViewModels
{
    public class DashboardViewModel : BaseViewModel
    {
        private readonly DashboardService _dashboardService = new DashboardService();

        private int _totalFoodItems;
        public int TotalFoodItems { get => _totalFoodItems; set => SetProperty(ref _totalFoodItems, value); }

        private int _expiringSoon;
        public int ExpiringSoon { get => _expiringSoon; set => SetProperty(ref _expiringSoon, value); }

        private int _totalRecipes;
        public int TotalRecipes { get => _totalRecipes; set => SetProperty(ref _totalRecipes, value); }

        private string _statusMessage = "";
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

        public RelayCommand RefreshCommand { get; }

        public DashboardViewModel()
        {
            RefreshCommand = new RelayCommand(Refresh);
            UserSession.CurrentUserChanged += Refresh;
            Refresh();
        }

        public void Refresh()
        {
            try
            {
                StatusMessage = "";
                var (total, expiring, recipes) = _dashboardService.GetStats();
                TotalFoodItems = total;
                ExpiringSoon = expiring;
                TotalRecipes = recipes;
            }
            catch (Exception ex)
            {
                StatusMessage = "Fehler beim Laden der Stats: " + ex.Message;
            }
        }
    }
}
