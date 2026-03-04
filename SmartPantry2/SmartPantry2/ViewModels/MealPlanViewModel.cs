using Smartpantry.Helpers;
using Smartpantry.Models;
using SmartPantry2.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace Smartpantry.ViewModels
{
    public class MealPlanViewModel : BaseViewModel
    {
        private readonly MealPlanService _mealPlanService = new MealPlanService();
        private readonly RecipeService _recipeService = new RecipeService();

        public ObservableCollection<MealPlan> Plans { get; } = new ObservableCollection<MealPlan>();
        public ObservableCollection<Recipe> AvailableRecipes { get; } = new ObservableCollection<Recipe>();

        private MealPlan? _selectedPlan;
        public MealPlan? SelectedPlan
        {
            get => _selectedPlan;
            set
            {
                if (SetProperty(ref _selectedPlan, value))
                    RemovePlanCommand.RaiseCanExecuteChanged();
            }
        }

        private Recipe? _selectedRecipe;
        public Recipe? SelectedRecipe
        {
            get => _selectedRecipe;
            set
            {
                if (SetProperty(ref _selectedRecipe, value))
                    AddPlanCommand.RaiseCanExecuteChanged();
            }
        }

        private DateTime _date = DateTime.Today;
        public DateTime Date { get => _date; set => SetProperty(ref _date, value); }

        private string _mealType = "Dinner";
        public string MealType { get => _mealType; set => SetProperty(ref _mealType, value); }

        private string _statusMessage = "";
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

        public RelayCommand LoadCommand { get; }
        public RelayCommand AddPlanCommand { get; }
        public RelayCommand RemovePlanCommand { get; }

        public MealPlanViewModel()
        {
            LoadCommand = new RelayCommand(Load);
            AddPlanCommand = new RelayCommand(AddPlan, CanAddPlan);
            RemovePlanCommand = new RelayCommand(RemovePlan, () => SelectedPlan != null);

            UserSession.CurrentUserChanged += () =>
            {
                Load();
                AddPlanCommand.RaiseCanExecuteChanged();
            };

            Load();
        }

        private bool CanAddPlan() =>
            UserSession.CurrentUserId != null && SelectedRecipe != null;

        public void Load()
        {
            try
            {
                StatusMessage = "";
                Plans.Clear();
                AvailableRecipes.Clear();

                var userId = UserSession.CurrentUserId;
                if (userId == null) return;

                var plans = _mealPlanService.GetWeekPlan(userId.Value);
                foreach (var p in plans.OrderBy(p => p.Date))
                    Plans.Add(p);

                var recipes = _recipeService.GetAll()
                    .Where(r => r.UserId == userId.Value)
                    .OrderBy(r => r.Name);

                foreach (var r in recipes)
                    AvailableRecipes.Add(r);
            }
            catch (Exception ex)
            {
                StatusMessage = "Fehler beim Laden des MealPlans: " + ex.Message;
            }
        }

        private void AddPlan()
        {
            try
            {
                StatusMessage = "";
                var userId = UserSession.CurrentUserId;
                if (userId == null || SelectedRecipe == null)
                {
                    StatusMessage = "Bitte einloggen und ein Rezept auswählen.";
                    return;
                }

                var plan = new MealPlan
                {
                    UserId = userId.Value,
                    RecipeId = SelectedRecipe.Id,
                    Date = Date.Date,
                    MealType = MealType ?? "Dinner"
                };

                _mealPlanService.Add(plan);
                Load();
                StatusMessage = "MealPlan hinzugefügt.";
            }
            catch (Exception ex)
            {
                StatusMessage = "Fehler beim Hinzufügen: " + ex.Message;
            }
        }

        private void RemovePlan()
        {
            try
            {
                if (SelectedPlan == null) return;

                StatusMessage = "";
                _mealPlanService.Remove(SelectedPlan.Id);
                Load();
                StatusMessage = "MealPlan entfernt.";
            }
            catch (Exception ex)
            {
                StatusMessage = "Fehler beim Entfernen: " + ex.Message;
            }
        }
    }
}
