// ------------------------------------------------------------
// Datei: RecipesViewModel.cs
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
using System.Windows;
using Microsoft.Win32;

namespace Smartpantry.ViewModels
{



    public class RecipesViewModel : BaseViewModel
    {
        // Dieses ViewModel steuert die komplette Rezepte-Seite.
        // Dazu gehoeren die Rezeptliste, das Formular, die Zutaten und die Bildauswahl.
        private readonly RecipeService _recipeService = new RecipeService();


        public ObservableCollection<Recipe> Recipes { get; } = new ObservableCollection<Recipe>();


        public ObservableCollection<RecipeIngredient> Ingredients { get; } = new ObservableCollection<RecipeIngredient>();

        public bool CanEdit => UserSession.IsAdmin;



        public bool IsSelectionMode { get => _isSelectionMode; private set => SetProperty(ref _isSelectionMode, value); }
        private bool _isSelectionMode;
        private Action<Recipe>? _recipeChosen;

        private Recipe? _selectedRecipe;


        public Recipe? SelectedRecipe
        {
            get => _selectedRecipe;
            set
            {
                if (SetProperty(ref _selectedRecipe, value))
                {




                    if (IsSelectionMode && value != null)
                    {
                        _recipeChosen?.Invoke(value);
                        return;
                    }

                    Ingredients.Clear();

                    if (value != null)
                    {
                        RecipeName = value.Name;
                        Description = value.Description;
                        Instructions = value.Instructions;
                        SelectedImagePath = value.ImagePath ?? "";

                        if (value.Ingredients != null)
                        {
                            foreach (var ing in value.Ingredients)
                                Ingredients.Add(ing);
                        }
                    }

                    NewRecipeCommand.RaiseCanExecuteChanged();
                    AddIngredientCommand.RaiseCanExecuteChanged();
                    RemoveIngredientCommand.RaiseCanExecuteChanged();
                    PickFoodForIngredientCommand.RaiseCanExecuteChanged();
                    PickImageCommand.RaiseCanExecuteChanged();
                }
            }
        }

        private RecipeIngredient? _selectedIngredient;
        public RecipeIngredient? SelectedIngredient
        {
            get => _selectedIngredient;
            set
            {
                if (SetProperty(ref _selectedIngredient, value))
                    RemoveIngredientCommand.RaiseCanExecuteChanged();
            }
        }

        private string _recipeName = "";
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
        public string Description { get => _description; set => SetProperty(ref _description, value); }

        private string _instructions = "";
        public string Instructions { get => _instructions; set => SetProperty(ref _instructions, value); }

        private string _selectedImagePath = "";
        public string SelectedImagePath { get => _selectedImagePath; set => SetProperty(ref _selectedImagePath, value); }

        private string _newIngredientName = "";
        public string NewIngredientName
        {
            get => _newIngredientName;
            set
            {
                if (SetProperty(ref _newIngredientName, value))
                    NewRecipeCommand.RaiseCanExecuteChanged();
                AddIngredientCommand.RaiseCanExecuteChanged();
            }
        }

        private decimal _newIngredientAmount = 0;
        public decimal NewIngredientAmount { get => _newIngredientAmount; set => SetProperty(ref _newIngredientAmount, value); }

        public ObservableCollection<string> IngredientUnits { get; } = new ObservableCollection<string>(
            new List<string> { "", "g", "ml", "Stück", "EL", "TL", "Messerspitze" });

        private string _selectedIngredientUnit = "";
        public string SelectedIngredientUnit { get => _selectedIngredientUnit; set => SetProperty(ref _selectedIngredientUnit, value); }

        private string _statusMessage = "";
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

        public RelayCommand LoadCommand { get; }
        public RelayCommand AddRecipeCommand { get; }
        public RelayCommand NewRecipeCommand { get; }

        public RelayCommand AddIngredientCommand { get; }
        public RelayCommand RemoveIngredientCommand { get; }


        public RelayCommand<Recipe> DeleteRecipeTileCommand { get; }
        public RelayCommand<Recipe> ChooseRecipeCommand { get; }


        public RelayCommand PickFoodForIngredientCommand { get; }
        public event Action? RequestPickFood;


        public RelayCommand PickImageCommand { get; }



        public RecipesViewModel()
        {
            LoadCommand = new RelayCommand(Load);
            AddRecipeCommand = new RelayCommand(AddRecipe, CanAddRecipe);
            NewRecipeCommand = new RelayCommand(NewRecipe, () => CanEdit);

            AddIngredientCommand = new RelayCommand(AddIngredient, CanAddIngredient);
            RemoveIngredientCommand = new RelayCommand(RemoveIngredient, () => SelectedIngredient != null);

            DeleteRecipeTileCommand = new RelayCommand<Recipe>(DeleteRecipeTile, recipe => CanEdit && recipe != null);
            ChooseRecipeCommand = new RelayCommand<Recipe>(ChooseRecipe, recipe => recipe != null);

            PickFoodForIngredientCommand = new RelayCommand(() => RequestPickFood?.Invoke(), () => CanEdit);

            PickImageCommand = new RelayCommand(PickImage, () => CanEdit);

            UserSession.CurrentUserChanged += () =>
            {
                Load();
                OnPropertyChanged(nameof(CanEdit));
                AddRecipeCommand.RaiseCanExecuteChanged();
                NewRecipeCommand.RaiseCanExecuteChanged();
                AddIngredientCommand.RaiseCanExecuteChanged();
                RemoveIngredientCommand.RaiseCanExecuteChanged();
                PickFoodForIngredientCommand.RaiseCanExecuteChanged();
                PickImageCommand.RaiseCanExecuteChanged();
                DeleteRecipeTileCommand.RaiseCanExecuteChanged();
                ChooseRecipeCommand.RaiseCanExecuteChanged();
            };

            Load();
        }




        public void StartSelectionMode(Action<Recipe> onChosen)
        {
            _recipeChosen = onChosen;
            IsSelectionMode = true;
        }

        public void EndSelectionMode()
        {
            IsSelectionMode = false;
            _recipeChosen = null;
        }






        private void ChooseRecipe(Recipe? recipe)
        {
            if (recipe == null) return;

            if (IsSelectionMode)
            {
                _recipeChosen?.Invoke(recipe);
                return;
            }

            SelectedRecipe = recipe;
        }





        private void NewRecipe()
        {
            SelectedRecipe = null;
            RecipeName = "";
            Description = "";
            Instructions = "";
            SelectedImagePath = "";
            Ingredients.Clear();
            SelectedIngredient = null;
            NewIngredientName = "";
            NewIngredientAmount = 0;
            SelectedIngredientUnit = "";
            StatusMessage = "Neues Rezept.";
        }

        private bool CanAddRecipe() =>
            UserSession.CurrentUserId != null && CanEdit && !string.IsNullOrWhiteSpace(RecipeName);

        private bool CanAddIngredient() =>
            CanEdit && !string.IsNullOrWhiteSpace(NewIngredientName);


        public void SetPickedFoodName(string foodName)
        {
            NewIngredientName = foodName;
        }


        public void Load()
        {
            try
            {
                StatusMessage = "";
                Recipes.Clear();

                var all = _recipeService.GetAll();
                var userId = UserSession.CurrentUserId;

                var filtered = userId == null || !UserSession.IsAdmin
                    ? Enumerable.Empty<Recipe>()
                    : all.Where(r => r.UserId == userId.Value);

                foreach (var r in filtered.OrderByDescending(r => r.CreatedAt))
                    Recipes.Add(r);
            }
            catch (Exception ex)
            {
                StatusMessage = "Fehler beim Laden der Rezepte: " + ex.Message;
            }
        }







        private void AddRecipe()
        {
            // Hier wird entschieden, ob ein neues Rezept angelegt
            // oder ein bereits vorhandenes Rezept aktualisiert wird.
            try
            {
                StatusMessage = "";
                var userId = UserSession.CurrentUserId;
                if (userId == null)
                {
                    StatusMessage = "Bitte zuerst einloggen.";
                    return;
                }

                if (SelectedRecipe != null)
                {

                    var selectedId = SelectedRecipe.Id;
                    SelectedRecipe.Name = RecipeName.Trim();
                    SelectedRecipe.Description = Description ?? "";
                    SelectedRecipe.Instructions = Instructions ?? "";
                    SelectedRecipe.Ingredients = Ingredients.ToList();
                    _recipeService.Update(SelectedRecipe);
                    Load();
                    SelectedRecipe = Recipes.FirstOrDefault(r => r.Id == selectedId);
                    StatusMessage = "Rezept gespeichert.";
                    return;
                }

                var recipe = new Recipe
                {
                    UserId = userId.Value,
                    Name = RecipeName.Trim(),
                    Description = Description ?? "",
                    Instructions = Instructions ?? "",
                    CreatedAt = DateTime.Now,
                    Ingredients = Ingredients.ToList(),
                    ImagePath = string.IsNullOrWhiteSpace(SelectedImagePath) ? null : SelectedImagePath
                };

                _recipeService.Add(recipe);
                Load();
                RecipeName = "";
                Description = "";
                Instructions = "";
                SelectedImagePath = "";
                Ingredients.Clear();
                SelectedRecipe = null;
                StatusMessage = "Rezept hinzugefügt.";
            }
            catch (Exception ex)
            {
                StatusMessage = "Fehler beim Hinzufügen: " + ex.Message;
            }
        }


        private void DeleteRecipeTile(Recipe? recipe)
        {
            try
            {
                if (recipe == null) return;

                var confirm = MessageBox.Show(
                    $"Rezept '{recipe.Name}' wirklich löschen?",
                    "Löschen bestätigen",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (confirm != MessageBoxResult.Yes)
                    return;

                _recipeService.Delete(recipe.Id);
                if (SelectedRecipe?.Id == recipe.Id)
                {
                    SelectedRecipe = null;
                    RecipeName = "";
                    Description = "";
                    Instructions = "";
                    SelectedImagePath = "";
                    Ingredients.Clear();
                }

                Load();
            }
            catch (Exception ex)
            {
                StatusMessage = "Fehler beim Löschen: " + ex.Message;
            }
        }





        private void AddIngredient()
        {
            try
            {
                StatusMessage = "";
                if (string.IsNullOrWhiteSpace(NewIngredientName))
                {
                    StatusMessage = "Bitte zuerst eine Zutat auswählen oder eingeben.";
                    return;
                }

                var ingredient = new RecipeIngredient
                {
                    RecipeId = SelectedRecipe?.Id ?? 0,
                    FoodItem = NewIngredientName.Trim(),
                    Amount = NewIngredientAmount <= 0 ? 0 : NewIngredientAmount,
                    Unit = SelectedIngredientUnit ?? ""
                };

                Ingredients.Add(ingredient);

                if (SelectedRecipe != null)
                {
                    SelectedRecipe.Ingredients = Ingredients.ToList();
                    _recipeService.Update(SelectedRecipe);
                    var selectedId = SelectedRecipe.Id;
                    Load();
                    SelectedRecipe = Recipes.FirstOrDefault(r => r.Id == selectedId);
                }

                NewIngredientName = "";
                NewIngredientAmount = 0;
                SelectedIngredientUnit = "";
                StatusMessage = "Zutat hinzugefügt.";
            }
            catch (Exception ex)
            {
                StatusMessage = "Fehler beim Hinzufügen der Zutat: " + ex.Message;
            }
        }

        private void RemoveIngredient()
        {
            try
            {
                if (SelectedIngredient == null) return;

                var removed = SelectedIngredient;
                Ingredients.Remove(removed);
                SelectedIngredient = null;

                if (SelectedRecipe != null)
                {
                    SelectedRecipe.Ingredients = Ingredients.ToList();
                    _recipeService.Update(SelectedRecipe);
                    var selectedId = SelectedRecipe.Id;
                    Load();
                    SelectedRecipe = Recipes.FirstOrDefault(r => r.Id == selectedId);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "Fehler beim Entfernen: " + ex.Message;
            }
        }


        private void PickImage()
        {
            try
            {
                var dlg = new OpenFileDialog
                {
                    Title = "Bild auswählen",
                    Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.gif",
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
                };

                if (dlg.ShowDialog() == true)
                {
                    SelectedImagePath = dlg.FileName;
                    if (SelectedRecipe != null) SelectedRecipe.ImagePath = dlg.FileName;
                    StatusMessage = "Bild ausgewählt.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "Fehler beim Bild-Auswählen: " + ex.Message;
            }
        }
    }
}
