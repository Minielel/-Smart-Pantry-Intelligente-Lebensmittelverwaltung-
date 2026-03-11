// ------------------------------------------------------------
// Datei: MealPlanViewModel.cs
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
using Smartpantry.Models;
using SmartPantry2.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Smartpantry.ViewModels
{
    public class MealPlanViewModel : BaseViewModel
    {
        private readonly MealPlanService _mealPlanService = new MealPlanService();

        public ObservableCollection<MealPlan> Plans { get; } = new ObservableCollection<MealPlan>();

        public bool CanEdit => UserSession.IsAdmin;

        public sealed class MealTypeOption
        {
            public string Value { get; set; } = "dinner";
            public string Label { get; set; } = "Abends";
        }


        public IReadOnlyList<MealTypeOption> MealTypes { get; } = new List<MealTypeOption>
        {
            new MealTypeOption { Value = "breakfast", Label = "Morgens" },
            new MealTypeOption { Value = "lunch", Label = "Mittags" },
            new MealTypeOption { Value = "dinner", Label = "Abends" },
        };

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

        private string _mealType = "dinner";
        public string MealType { get => _mealType; set => SetProperty(ref _mealType, value); }

        private string _statusMessage = "";
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

        public RelayCommand LoadCommand { get; }
        public RelayCommand AddPlanCommand { get; }
        public RelayCommand RemovePlanCommand { get; }

        public RelayCommand PickRecipeCommand { get; }
        public event Action? RequestPickRecipe;

        public MealPlanViewModel()
        {
            LoadCommand = new RelayCommand(Load);
            AddPlanCommand = new RelayCommand(AddPlan, CanAddPlan);
            RemovePlanCommand = new RelayCommand(RemovePlan, () => CanEdit && SelectedPlan != null);

            PickRecipeCommand = new RelayCommand(() => RequestPickRecipe?.Invoke(), () => UserSession.CurrentUserId != null && CanEdit);

            UserSession.CurrentUserChanged += () =>
            {
                Load();
                OnPropertyChanged(nameof(CanEdit));
                AddPlanCommand.RaiseCanExecuteChanged();
                RemovePlanCommand.RaiseCanExecuteChanged();
                PickRecipeCommand.RaiseCanExecuteChanged();
            };

            Load();
        }

        private bool CanAddPlan() =>
            UserSession.CurrentUserId != null && CanEdit && SelectedRecipe != null;

        public void SetPickedRecipe(Recipe recipe)
        {
            SelectedRecipe = recipe;
        }

        public void Load()
        {
            try
            {
                StatusMessage = "";
                Plans.Clear();

                var userId = UserSession.CurrentUserId;
                if (userId == null) return;

                var plans = _mealPlanService.GetWeekPlan(userId.Value);
                foreach (var p in plans.OrderBy(p => p.Date))
                    Plans.Add(p);
            }
            catch (Exception ex)
            {
                StatusMessage = "Fehler beim Laden des Plans: " + ex.Message;
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
                    MealType = MealType ?? "dinner"
                };

                _mealPlanService.Add(plan);
                Load();
                StatusMessage = "Plan hinzugefügt.";
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
            }
            catch (Exception ex)
            {
                StatusMessage = "Fehler beim Entfernen: " + ex.Message;
            }
        }
    }
}
