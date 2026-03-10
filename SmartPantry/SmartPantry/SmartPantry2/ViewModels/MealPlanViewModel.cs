// ============================================================
// Datei:   MealPlanViewModel.cs
// Schicht: ViewModel / Wochenplanung
//
// ZWECK:
//   Verwaltet den Wochenplan: Einträge laden, hinzufügen, entfernen.
//   Kommuniziert über Events mit RecipesViewModel für die Rezeptauswahl.
//
// ROTER FADEN:
//   MealPlanView.xaml ←→ MealPlanViewModel ←→ MealPlanService ←→ DB: meal_plan
//
//   REZEPT AUSWÄHLEN (seitenübergreifend):
//   User klickt "Rezept auswählen"
//     → PickRecipeCommand → RequestPickRecipe-Event feuert
//     → MainViewModel reagiert: RecipesVM.StartSelectionMode(callback)
//     → navigiert zu RecipesView
//     → User klickt Rezept → SetPickedRecipe(recipe)
//     → SelectedRecipe = recipe, RecipeName = recipe.Name
//     → User wählt Datum + MealType + klickt "Hinzufügen"
//
//   MealType-Werte:
//   DB speichert: "breakfast" | "lunch" | "dinner"
//   MealPlanView.xaml zeigt per DataTrigger: "Morgens" | "Mittags" | "Abends"
//   Quelle DataTrigger: https://learn.microsoft.com/dotnet/desktop/wpf/data/data-templating-overview
//
// USER USECASE:
//   Admin: "Rezept auswählen" → Rezept wählen → zurück zur Planseite
//   Admin: Datum + Mahlzeit (Morgens/Mittags/Abends) wählen
//   Admin: "Hinzufügen" → Eintrag erscheint in der Plan-Liste
//   Admin: Eintrag in der Liste anklicken → "Löschen" → weg
//
// QUELLEN:
//   ObservableCollection<T>:
//   https://learn.microsoft.com/dotnet/api/system.collections.objectmodel.observablecollection-1
//
//   C# Events für ViewModel-Kommunikation:
//   https://learn.microsoft.com/dotnet/csharp/programming-guide/events/
//
//   DatePicker WPF Control:
//   https://learn.microsoft.com/dotnet/desktop/wpf/controls/datepicker
// ============================================================

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
        // MealPlanService: CRUD für meal_plan-Tabelle
        private readonly MealPlanService _mealPlanService = new MealPlanService();

        // ── PLAN-LISTE ────────────────────────────────────────────────────────────
        // Alle Wochenplan-Einträge des Users (inkl. Recipe-Navigation per Include())
        private ObservableCollection<MealPlan> _mealPlans = new();
        public ObservableCollection<MealPlan> MealPlans
        {
            get => _mealPlans;
            private set => SetProperty(ref _mealPlans, value);
        }

        // ── AUSGEWÄHLTER EINTRAG (für Lösch-Button) ───────────────────────────────
        private MealPlan? _selectedPlan;
        public MealPlan? SelectedPlan
        {
            get => _selectedPlan;
            set
            {
                if (SetProperty(ref _selectedPlan, value))
                    // Lösch-Button neu prüfen (aktiv wenn ein Eintrag ausgewählt)
                    RemovePlanCommand.RaiseCanExecuteChanged();
            }
        }

        // ── FORMULAR-FELDER ────────────────────────────────────────────────────────
        // Ausgewähltes Rezept (aus RecipesView per Auswahlmodus übernommen)
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

        // Name des ausgewählten Rezepts (für Anzeige im Formular)
        private string _selectedRecipeName = "(kein Rezept gewählt)";
        public string SelectedRecipeName
        {
            get => _selectedRecipeName;
            set => SetProperty(ref _selectedRecipeName, value);
        }

        // Datum für den Planeintrag (DatePicker-Binding)
        // Standardwert: morgen (nächster Tag für vorausschauende Planung)
        private DateTime _selectedDate = DateTime.Today.AddDays(1);
        public DateTime SelectedDate
        {
            get => _selectedDate;
            set => SetProperty(ref _selectedDate, value);
        }

        // Mahlzeittyp: "breakfast" | "lunch" | "dinner"
        // Wird per ComboBox in MealPlanView.xaml ausgewählt
        // DB-Spalte: "meal_type" (ENUM)
        private string _selectedMealType = "breakfast";
        public string SelectedMealType
        {
            get => _selectedMealType;
            set => SetProperty(ref _selectedMealType, value);
        }

        // ── VERFÜGBARE MAHLZEITTYPEN (für ComboBox) ───────────────────────────────
        // Diese Liste wird an die ComboBox in MealPlanView.xaml gebunden
        // DataTrigger in der View übersetzt auf Deutsch ("breakfast" → "Morgens")
        public string[] MealTypes { get; } = { "breakfast", "lunch", "dinner" };

        // ── EVENT FÜR SEITENÜBERGREIFENDE KOMMUNIKATION ───────────────────────────
        // MainViewModel abonniert dieses Event und wechselt zu RecipesView
        public event Action? RequestPickRecipe;

        private bool _canEdit => UserSession.IsAdmin;

        private string _statusMessage = "";
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        // ── COMMANDS ──────────────────────────────────────────────────────────────
        // Eintrag zum Wochenplan hinzufügen
        public RelayCommand AddPlanCommand { get; }

        // Ausgewählten Eintrag löschen
        public RelayCommand RemovePlanCommand { get; }

        // "Rezept auswählen"-Button: feuert RequestPickRecipe
        public RelayCommand PickRecipeCommand { get; }

        public MealPlanViewModel()
        {
            AddPlanCommand = new RelayCommand(
                AddPlan,
                // Aktiv wenn Admin UND Rezept ausgewählt
                () => _canEdit && SelectedRecipe != null);

            RemovePlanCommand = new RelayCommand(
                RemovePlan,
                // Aktiv wenn Admin UND Planeintrag ausgewählt
                () => _canEdit && SelectedPlan != null);

            // PickRecipeCommand: feuert Event → MainViewModel reagiert
            PickRecipeCommand = new RelayCommand(
                () => RequestPickRecipe?.Invoke(),
                () => _canEdit);

            // Bei Login/Logout neu laden
            UserSession.CurrentUserChanged += Load;

            Load();
        }

        // --------------------------------------------------------
        // Load
        //
        // FUNKTION: lädt alle Wochenplan-Einträge des Users aus DB
        //
        // MealPlanService.GetWeekPlan():
        //   SELECT meal_plan.*, recipes.* FROM meal_plan
        //   INNER JOIN recipes ON meal_plan.recipe_id = recipes.id
        //   WHERE meal_plan.user_id = ?
        //
        // Include(m => m.Recipe): Recipe.Name für Anzeige in MealPlanView.xaml
        // --------------------------------------------------------
        private void Load()
        {
            if (UserSession.CurrentUserId == null)
            {
                MealPlans = new ObservableCollection<MealPlan>();
                return;
            }

            try
            {
                var plans = _mealPlanService.GetWeekPlan(UserSession.CurrentUserId.Value);
                // Nach Datum sortieren: chronologische Anzeige im Plan
                MealPlans = new ObservableCollection<MealPlan>(
                    plans.OrderBy(p => p.Date).ThenBy(p => p.MealType));
            }
            catch (Exception ex)
            {
                StatusMessage = "Fehler beim Laden: " + ex.Message;
            }
        }

        // --------------------------------------------------------
        // AddPlan
        //
        // FUNKTION: fügt neuen Wochenplan-Eintrag in die DB ein
        //
        // DB: INSERT INTO meal_plan (user_id, recipe_id, date, meal_type)
        // --------------------------------------------------------
        private void AddPlan()
        {
            if (SelectedRecipe == null) return;

            try
            {
                var plan = new MealPlan
                {
                    UserId    = UserSession.CurrentUserId!.Value,
                    RecipeId  = SelectedRecipe.Id,
                    // .Date: nur Datum ohne Uhrzeit → konsistent mit DATE-Typ in DB
                    Date      = SelectedDate.Date,
                    MealType  = SelectedMealType
                };

                // MealPlanService.Add() → INSERT INTO meal_plan
                _mealPlanService.Add(plan);

                // Liste neu laden damit neuer Eintrag erscheint
                Load();

                // Formular zurücksetzen
                SelectedRecipe     = null;
                SelectedRecipeName = "(kein Rezept gewählt)";
                SelectedDate       = DateTime.Today.AddDays(1);
            }
            catch (Exception ex)
            {
                StatusMessage = "Fehler beim Hinzufügen: " + ex.Message;
            }
        }

        // --------------------------------------------------------
        // RemovePlan
        //
        // FUNKTION: löscht den ausgewählten Wochenplan-Eintrag
        //
        // DB: DELETE FROM meal_plan WHERE id=?
        // --------------------------------------------------------
        private void RemovePlan()
        {
            if (SelectedPlan == null) return;

            try
            {
                // MealPlanService.Remove() → DELETE FROM meal_plan WHERE id=?
                _mealPlanService.Remove(SelectedPlan.Id);
                Load();  // Liste aktualisieren
            }
            catch (Exception ex)
            {
                StatusMessage = "Fehler beim Löschen: " + ex.Message;
            }
        }

        // --------------------------------------------------------
        // SetPickedRecipe
        //
        // FUNKTION:
        //   Wird von MainViewModel aufgerufen nachdem User ein Rezept
        //   in RecipesView ausgewählt hat.
        //   Setzt SelectedRecipe und aktualisiert SelectedRecipeName für die Anzeige.
        //
        // PARAMETER: recipe → das gewählte Recipe-Objekt
        // --------------------------------------------------------
        public void SetPickedRecipe(Recipe recipe)
        {
            // Rezept-Objekt speichern (wird beim AddPlan verwendet)
            SelectedRecipe     = recipe;
            // Name für die Anzeige im Formular
            SelectedRecipeName = recipe.Name ?? "(unbenannt)";
        }
    }
}