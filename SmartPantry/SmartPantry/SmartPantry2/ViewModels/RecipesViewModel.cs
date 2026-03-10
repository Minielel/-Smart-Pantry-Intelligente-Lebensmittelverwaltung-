// ============================================================
// Datei:   RecipesViewModel.cs
// Schicht: ViewModel / Rezeptverwaltung
//
// ZWECK:
//   Steuert die Rezepte-Seite: Liste, Detailformular, Zutaten-Verwaltung,
//   Bildauswahl und Auswahlmodus für den Wochenplan.
//
// ROTER FADEN:
//   RecipesView.xaml ←→ RecipesViewModel ←→ RecipeService ←→ DB: recipes, recipe_ingredients
//
//   ZUTAT HINZUFÜGEN (seitenübergreifend):
//   User klickt "Zutat auswählen" → PickFoodForIngredientCommand
//     → RequestPickFood-Event feuert
//     → MainViewModel: FoodVM.StartSelectionMode(callback) + NavigateTo(FoodVM)
//     → User wählt Item in FoodView → callback(food)
//     → SetPickedFoodName(food.Name) → NewIngredientName-Textfeld befüllt
//
//   AUSWAHLMODUS (für Wochenplan):
//   MealPlanVM braucht ein Recipe-Objekt.
//   MainViewModel ruft RecipesVM.StartSelectionMode(callback) auf.
//   → IsSelectionMode = true → Klick auf Rezept → _recipeChosen(recipe)
//
//   AddRecipe() entscheidet:
//   SelectedRecipe != null → Update() (Bearbeitungsmodus)
//   SelectedRecipe == null → Add()    (Neuerstellungsmodus)
//
// USER USECASE ADMIN:
//   "+" klicken → Formular leeren (NewRecipe())
//   Name, Beschreibung, Anleitung eingeben
//   Zutaten hinzufügen (manuell oder aus Vorrat auswählen)
//   Optional: Bild auswählen (OpenImageCommand)
//   "Add" klicken → RecipeService.Add() → Rezept + Zutaten in DB
//   Bestehendes Rezept anklicken → Formular befüllt sich → "Add" → Update
//
// QUELLEN:
//   ObservableCollection<T>:
//   https://learn.microsoft.com/dotnet/api/system.collections.objectmodel.observablecollection-1
//
//   OpenFileDialog (Bildauswahl):
//   https://learn.microsoft.com/dotnet/api/microsoft.win32.openfiledialog
//
//   Recipe.ImagePath ist [NotMapped]: wird nicht in DB gespeichert!
//   Quelle: https://learn.microsoft.com/ef/core/modeling/entity-properties#excluded-properties
// ============================================================

using Microsoft.Win32;
using Smartpantry.Helpers;
using Smartpantry.Models;
using SmartPantry2.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace Smartpantry.ViewModels
{
    public class RecipesViewModel : BaseViewModel
    {
        // RecipeService: CRUD-Operationen für Rezepte + Zutaten in DB
        private readonly RecipeService _recipeService = new RecipeService();

        // ── REZEPTLISTE ────────────────────────────────────────────────────────────
        // ObservableCollection: WPF aktualisiert Liste automatisch bei Änderungen
        // Quelle: https://learn.microsoft.com/dotnet/api/system.collections.objectmodel.observablecollection-1
        private ObservableCollection<Recipe> _recipes = new();
        public ObservableCollection<Recipe> Recipes
        {
            get => _recipes;
            private set => SetProperty(ref _recipes, value);
        }

        // ── AUSGEWÄHLTES REZEPT (Bearbeitungsmodus) ────────────────────────────────
        private Recipe? _selectedRecipe;
        public Recipe? SelectedRecipe
        {
            get => _selectedRecipe;
            set
            {
                if (SetProperty(ref _selectedRecipe, value) && value != null)
                {
                    // Wenn Auswahlmodus aktiv: Callback aufrufen und abbrechen
                    if (IsSelectionMode)
                    {
                        _recipeChosen?.Invoke(value);
                        return;
                    }

                    // Normaler Modus: Formular mit Rezept-Daten befüllen
                    RecipeName    = value.Name        ?? "";
                    Description   = value.Description ?? "";
                    Instructions  = value.Instructions ?? "";
                    // Zutaten-Liste befüllen (aus Navigationsproperty)
                    CurrentIngredients = new ObservableCollection<RecipeIngredient>(
                        value.Ingredients ?? Enumerable.Empty<RecipeIngredient>());
                }
            }
        }

        // ── FORMULAR-FELDER ────────────────────────────────────────────────────────
        private string _recipeName = "";
        // Rezeptname (z.B. "Spaghetti Bolognese") → DB-Spalte "name"
        public string RecipeName
        {
            get => _recipeName;
            set
            {
                if (SetProperty(ref _recipeName, value))
                    AddRecipeCommand.RaiseCanExecuteChanged();
            }
        }

        private string _description = "";
        // Kurzbeschreibung → DB-Spalte "description" (TEXT)
        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        private string _instructions = "";
        // Schritt-für-Schritt-Anleitung → DB-Spalte "instructions" (TEXT)
        public string Instructions
        {
            get => _instructions;
            set => SetProperty(ref _instructions, value);
        }

        // ── ZUTATEN-VERWALTUNG ────────────────────────────────────────────────────
        // Liste der Zutaten des aktuell bearbeiteten Rezepts
        private ObservableCollection<RecipeIngredient> _currentIngredients = new();
        public ObservableCollection<RecipeIngredient> CurrentIngredients
        {
            get => _currentIngredients;
            set => SetProperty(ref _currentIngredients, value);
        }

        // Eingabefelder für neue Zutat
        private string _newIngredientName = "";
        // Name der neuen Zutat (kann manuell eingegeben oder aus FoodView übernommen werden)
        // ACHTUNG: wird gespeichert in DB als recipe_ingredients.food_item_name
        public string NewIngredientName
        {
            get => _newIngredientName;
            set
            {
                if (SetProperty(ref _newIngredientName, value))
                    AddIngredientCommand.RaiseCanExecuteChanged();
            }
        }

        private decimal _newIngredientAmount;
        // Menge der neuen Zutat → DB-Spalte "amount"
        public decimal NewIngredientAmount
        {
            get => _newIngredientAmount;
            set => SetProperty(ref _newIngredientAmount, value);
        }

        private string _newIngredientUnit = "g";
        // Einheit der neuen Zutat → DB-Spalte "unit"
        public string NewIngredientUnit
        {
            get => _newIngredientUnit;
            set => SetProperty(ref _newIngredientUnit, value);
        }

        // ── BILD ──────────────────────────────────────────────────────────────────
        // HINWEIS: ImagePath ist [NotMapped] in Recipe.cs → NICHT in DB gespeichert!
        // Wird nur im RAM gehalten; nach App-Neustart wieder null.
        private string? _selectedImagePath;
        public string? SelectedImagePath
        {
            get => _selectedImagePath;
            set => SetProperty(ref _selectedImagePath, value);
        }

        // ── AUSWAHLMODUS ──────────────────────────────────────────────────────────
        // true = MealPlanVM wartet auf Rezeptauswahl
        private bool _isSelectionMode;
        public bool IsSelectionMode
        {
            get => _isSelectionMode;
            private set => SetProperty(ref _isSelectionMode, value);
        }

        // Callback für die Rezeptauswahl (aus MealPlanVM über MainViewModel)
        private Action<Recipe>? _recipeChosen;

        // ── EVENTS FÜR SEITENÜBERGREIFENDE KOMMUNIKATION ─────────────────────────
        // RequestPickFood: gefeuert wenn User auf "Zutat auswählen" klickt
        // MainViewModel abonniert dieses Event und wechselt zu FoodView
        // Quelle: https://learn.microsoft.com/dotnet/csharp/programming-guide/events/
        public event Action? RequestPickFood;

        // ── BERECHTIGUNGEN ────────────────────────────────────────────────────────
        public bool CanEdit => UserSession.IsAdmin;

        private string _statusMessage = "";
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        // ── COMMANDS ──────────────────────────────────────────────────────────────
        // Rezept hinzufügen/speichern (Add oder Update je nach SelectedRecipe)
        public RelayCommand AddRecipeCommand { get; }

        // Zutat zur aktuellen Zutaten-Liste hinzufügen
        public RelayCommand AddIngredientCommand { get; }

        // Zutat aus der Liste entfernen
        public RelayCommand<RecipeIngredient> RemoveIngredientCommand { get; }

        // "×"-Button auf einer Rezept-Kachel: Rezept löschen
        public RelayCommand<Recipe> DeleteRecipeTileCommand { get; }

        // Formular leeren für ein neues Rezept
        public RelayCommand NewRecipeCommand { get; }

        // Öffnet OpenFileDialog für Bildauswahl
        public RelayCommand OpenImageCommand { get; }

        // "Zutat auswählen"-Button: feuert RequestPickFood-Event
        public RelayCommand PickFoodForIngredientCommand { get; }

        public RecipesViewModel()
        {
            AddRecipeCommand      = new RelayCommand(AddRecipe, () => CanEdit && !string.IsNullOrWhiteSpace(RecipeName));
            AddIngredientCommand  = new RelayCommand(AddIngredient, () => !string.IsNullOrWhiteSpace(NewIngredientName));
            RemoveIngredientCommand = new RelayCommand<RecipeIngredient>(RemoveIngredient);
            DeleteRecipeTileCommand = new RelayCommand<Recipe>(DeleteRecipeTile, r => CanEdit && r != null);
            NewRecipeCommand      = new RelayCommand(NewRecipe);
            OpenImageCommand      = new RelayCommand(OpenImageDialog);

            // PickFoodForIngredientCommand: teilt MainViewModel mit "Ich brauche ein Food-Item"
            PickFoodForIngredientCommand = new RelayCommand(
                () => RequestPickFood?.Invoke(),
                // Nur ausführbar wenn CanEdit (Admin)
                () => CanEdit);

            // Bei Login/Logout: neu laden + Berechtigungen aktualisieren
            UserSession.CurrentUserChanged += () =>
            {
                OnPropertyChanged(nameof(CanEdit));
                AddRecipeCommand.RaiseCanExecuteChanged();
                DeleteRecipeTileCommand.RaiseCanExecuteChanged();
                Load();
            };

            Load();
        }

        // --------------------------------------------------------
        // Load
        //
        // FUNKTION: lädt alle Rezepte des Users inkl. Zutaten aus DB
        //
        // RecipeService.GetAll() → SELECT recipes + recipe_ingredients
        // Filtert auf aktuellen User (und ggf. Admin sieht alle)
        // --------------------------------------------------------
        private void Load()
        {
            if (UserSession.CurrentUserId == null)
            {
                Recipes = new ObservableCollection<Recipe>();
                return;
            }

            try
            {
                var userId = UserSession.CurrentUserId.Value;

                // RecipeService.GetAll(): lädt alle Rezepte mit Zutaten per Include()
                var allRecipes = _recipeService.GetAll();

                // Admin sieht alle Rezepte, Standard-User nur seine eigenen
                var filtered = UserSession.IsAdmin
                    ? allRecipes
                    : allRecipes.Where(r => r.UserId == userId).ToList();

                Recipes = new ObservableCollection<Recipe>(filtered);
            }
            catch (Exception ex)
            {
                StatusMessage = "Fehler beim Laden: " + ex.Message;
            }
        }

        // --------------------------------------------------------
        // AddRecipe
        //
        // FUNKTION:
        //   Add wenn SelectedRecipe = null (neues Rezept)
        //   Update wenn SelectedRecipe != null (vorhandenes Rezept bearbeiten)
        //
        // Zutaten werden aus CurrentIngredients-Liste übernommen.
        // Nach Speichern: Formular leeren, Liste neu laden.
        // --------------------------------------------------------
        private void AddRecipe()
        {
            try
            {
                if (SelectedRecipe == null)
                {
                    // ── NEUES REZEPT ──────────────────────────────────────────────
                    var recipe = new Recipe
                    {
                        UserId       = UserSession.CurrentUserId!.Value,
                        Name         = RecipeName.Trim(),
                        Description  = Description.Trim(),
                        Instructions = Instructions.Trim(),
                        CreatedAt    = DateTime.Now,
                        ImagePath    = SelectedImagePath  // [NotMapped] → nur im RAM
                    };

                    // Zutaten aus der Liste dem Rezept hinzufügen
                    // EF Core erkennt die Ingredients-Collection automatisch
                    foreach (var ing in CurrentIngredients)
                        recipe.Ingredients.Add(ing);

                    // RecipeService.Add() → INSERT INTO recipes + recipe_ingredients
                    _recipeService.Add(recipe);
                }
                else
                {
                    // ── BESTEHENDES REZEPT AKTUALISIEREN ─────────────────────────
                    SelectedRecipe.Name         = RecipeName.Trim();
                    SelectedRecipe.Description  = Description.Trim();
                    SelectedRecipe.Instructions = Instructions.Trim();
                    SelectedRecipe.ImagePath    = SelectedImagePath;

                    // Zutaten-Liste ersetzen (EF Core erkennt Änderungen)
                    SelectedRecipe.Ingredients = CurrentIngredients.ToList();

                    // RecipeService.Update() → UPDATE recipes + DELETE/INSERT ingredients
                    _recipeService.Update(SelectedRecipe);
                }

                NewRecipe();  // Formular leeren
                Load();       // Liste aktualisieren
                StatusMessage = "";
            }
            catch (Exception ex)
            {
                StatusMessage = "Fehler beim Speichern: " + ex.Message;
            }
        }

        // --------------------------------------------------------
        // AddIngredient
        //
        // FUNKTION:
        //   Erstellt RecipeIngredient-Objekt aus den Eingabefeldern
        //   und fügt es zur CurrentIngredients-Liste hinzu.
        //   Wird NOCH NICHT in DB gespeichert (erst bei AddRecipe/Update).
        // --------------------------------------------------------
        private void AddIngredient()
        {
            // Neues Zutaten-Objekt aus Formular-Daten
            var ing = new RecipeIngredient
            {
                // FoodItem (C#) = food_item_name (DB) – Mapping in FoodDbContext!
                FoodItem = NewIngredientName.Trim(),
                Amount   = NewIngredientAmount,
                Unit     = NewIngredientUnit.Trim()
            };

            // Zur Anzeige-Liste hinzufügen (ObservableCollection aktualisiert UI sofort)
            CurrentIngredients.Add(ing);

            // Zutaten-Eingabefelder leeren
            NewIngredientName   = "";
            NewIngredientAmount = 0;
            NewIngredientUnit   = "g";
        }

        // --------------------------------------------------------
        // RemoveIngredient
        //
        // FUNKTION: entfernt eine Zutat aus der CurrentIngredients-Liste
        //   (noch keine DB-Änderung, erst bei AddRecipe/Update wirksam)
        // --------------------------------------------------------
        private void RemoveIngredient(RecipeIngredient? ing)
        {
            if (ing == null) return;
            CurrentIngredients.Remove(ing);
        }

        // --------------------------------------------------------
        // DeleteRecipeTile
        //
        // FUNKTION: löscht ein Rezept nach Bestätigung dauerhaft aus DB
        //   ON DELETE CASCADE im SQL löscht auch recipe_ingredients + meal_plan-Einträge!
        // --------------------------------------------------------
        private void DeleteRecipeTile(Recipe? recipe)
        {
            if (recipe == null) return;

            var result = MessageBox.Show(
                $"Rezept '{recipe.Name}' wirklich löschen?",
                "Löschen bestätigen",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                // RecipeService.Delete() → DELETE FROM recipes WHERE id=?
                // CASCADE: recipe_ingredients + meal_plan-Einträge werden mitgelöscht
                _recipeService.Delete(recipe.Id);
                Load();  // Liste aktualisieren
            }
            catch (Exception ex)
            {
                StatusMessage = "Fehler beim Löschen: " + ex.Message;
            }
        }

        // --------------------------------------------------------
        // NewRecipe
        //
        // FUNKTION: leert alle Formular-Felder für ein neues Rezept
        //   Setzt SelectedRecipe = null → nächster "Add" erstellt neues Rezept
        // --------------------------------------------------------
        private void NewRecipe()
        {
            SelectedRecipe     = null;
            RecipeName         = "";
            Description        = "";
            Instructions       = "";
            SelectedImagePath  = null;
            // Zutaten-Liste leeren
            CurrentIngredients = new ObservableCollection<RecipeIngredient>();
            NewIngredientName  = "";
            NewIngredientAmount = 0;
            NewIngredientUnit  = "g";
        }

        // --------------------------------------------------------
        // OpenImageDialog
        //
        // FUNKTION:
        //   Öffnet einen Datei-Dialog zur Bildauswahl.
        //   Setzt SelectedImagePath auf den gewählten Pfad.
        //   HINWEIS: Pfad wird nur im RAM gespeichert ([NotMapped])!
        //
        // Quelle OpenFileDialog (Microsoft.Win32):
        //   https://learn.microsoft.com/dotnet/api/microsoft.win32.openfiledialog
        // --------------------------------------------------------
        private void OpenImageDialog()
        {
            // OpenFileDialog: Datei-Auswahl-Dialog von Windows
            // Quelle: https://learn.microsoft.com/dotnet/api/microsoft.win32.openfiledialog
            var dlg = new OpenFileDialog
            {
                // Nur Bilddateien anzeigen
                Filter = "Bilder|*.jpg;*.jpeg;*.png;*.bmp;*.gif",
                Title  = "Rezeptbild auswählen"
            };

            // ShowDialog(): blockiert bis User Auswahl bestätigt oder abbricht
            // "?? false" = wenn ShowDialog null zurückgibt → false
            if (dlg.ShowDialog() ?? false)
            {
                // Pfad des gewählten Bildes speichern
                SelectedImagePath = dlg.FileName;
            }
        }

        // --------------------------------------------------------
        // SetPickedFoodName
        //
        // FUNKTION:
        //   Wird von MainViewModel aufgerufen nachdem User ein FoodItem
        //   im FoodView-Auswahlmodus gewählt hat.
        //   Trägt den Food-Namen in das Zutaten-Eingabefeld ein.
        //
        // PARAMETER: name → Name des gewählten FoodItems
        // --------------------------------------------------------
        public void SetPickedFoodName(string name)
        {
            // Name ins Zutaten-Namensfeld übertragen
            NewIngredientName = name;
        }

        // --------------------------------------------------------
        // StartSelectionMode / EndSelectionMode
        //
        // Auswahlmodus für MealPlanViewModel:
        // Wenn IsSelectionMode=true → jeder Klick auf Rezept ruft _recipeChosen auf
        // --------------------------------------------------------
        public void StartSelectionMode(Action<Recipe> onChosen)
        {
            _recipeChosen   = onChosen;
            IsSelectionMode = true;
        }

        public void EndSelectionMode()
        {
            _recipeChosen   = null;
            IsSelectionMode = false;
            SelectedRecipe  = null;
        }
    }
}