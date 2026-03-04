using Smartpantry.Helpers;
using Smartpantry.Models;
using SmartPantry2.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace Smartpantry.ViewModels
{
    public class RecipesViewModel : BaseViewModel
    {
        private readonly RecipeService _recipeService = new RecipeService();

        public ObservableCollection<Recipe> Recipes { get; } = new ObservableCollection<Recipe>();
        public ObservableCollection<RecipeIngredient> Ingredients { get; } = new ObservableCollection<RecipeIngredient>();

        private Recipe? _selectedRecipe;
        public Recipe? SelectedRecipe
        {
            get => _selectedRecipe;
            set
            {
                if (SetProperty(ref _selectedRecipe, value))
                {
                    Ingredients.Clear();

                    if (value != null)
                    {
                        RecipeName = value.Name;
                        Description = value.Description;
                        Instructions = value.Instructions;

                        if (value.Ingredients != null)
                        {
                            foreach (var ing in value.Ingredients)
                                Ingredients.Add(ing);
                        }
                    }

                    UpdateRecipeCommand.RaiseCanExecuteChanged();
                    DeleteRecipeCommand.RaiseCanExecuteChanged();
                    AddIngredientCommand.RaiseCanExecuteChanged();
                    RemoveIngredientCommand.RaiseCanExecuteChanged();
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

        private string _newIngredientName = "";
        public string NewIngredientName
        {
            get => _newIngredientName;
            set
            {
                if (SetProperty(ref _newIngredientName, value))
                    AddIngredientCommand.RaiseCanExecuteChanged();
            }
        }

        private decimal _newIngredientAmount = 1;
        public decimal NewIngredientAmount { get => _newIngredientAmount; set => SetProperty(ref _newIngredientAmount, value); }

        private string _newIngredientUnit = "";
        public string NewIngredientUnit { get => _newIngredientUnit; set => SetProperty(ref _newIngredientUnit, value); }

        private string _statusMessage = "";
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

        public RelayCommand LoadCommand { get; }
        public RelayCommand AddRecipeCommand { get; }
        public RelayCommand UpdateRecipeCommand { get; }
        public RelayCommand DeleteRecipeCommand { get; }
        public RelayCommand ClearFormCommand { get; }
        public RelayCommand AddIngredientCommand { get; }
        public RelayCommand RemoveIngredientCommand { get; }

        public RecipesViewModel()
        {
            LoadCommand = new RelayCommand(Load);
            AddRecipeCommand = new RelayCommand(AddRecipe, CanAddRecipe);
            UpdateRecipeCommand = new RelayCommand(UpdateRecipe, () => SelectedRecipe != null);
            DeleteRecipeCommand = new RelayCommand(DeleteRecipe, () => SelectedRecipe != null);
            ClearFormCommand = new RelayCommand(ClearForm);

            AddIngredientCommand = new RelayCommand(AddIngredient, CanAddIngredient);
            RemoveIngredientCommand = new RelayCommand(RemoveIngredient, () => SelectedIngredient != null);

            UserSession.CurrentUserChanged += () =>
            {
                ClearForm();
                Load();
                AddRecipeCommand.RaiseCanExecuteChanged();
            };

            Load();
        }

        private bool CanAddRecipe() =>
            UserSession.CurrentUserId != null && !string.IsNullOrWhiteSpace(RecipeName);

        private bool CanAddIngredient() =>
            SelectedRecipe != null && !string.IsNullOrWhiteSpace(NewIngredientName);

        public void Load()
        {
            try
            {
                StatusMessage = "";
                Recipes.Clear();

                var all = _recipeService.GetAll();
                var userId = UserSession.CurrentUserId;

                var filtered = userId == null
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
            try
            {
                StatusMessage = "";
                var userId = UserSession.CurrentUserId;
                if (userId == null)
                {
                    StatusMessage = "Bitte zuerst einloggen.";
                    return;
                }

                var recipe = new Recipe
                {
                    UserId = userId.Value,
                    Name = RecipeName.Trim(),
                    Description = Description ?? "",
                    Instructions = Instructions ?? "",
                    CreatedAt = DateTime.Now,
                    Ingredients = new System.Collections.Generic.List<RecipeIngredient>()
                };

                _recipeService.Add(recipe);
                Load();
                ClearForm();
                StatusMessage = "Rezept hinzugefügt.";
            }
            catch (Exception ex)
            {
                StatusMessage = "Fehler beim Hinzufügen: " + ex.Message;
            }
        }

        private void UpdateRecipe()
        {
            try
            {
                if (SelectedRecipe == null) return;

                StatusMessage = "";
                SelectedRecipe.Name = RecipeName.Trim();
                SelectedRecipe.Description = Description ?? "";
                SelectedRecipe.Instructions = Instructions ?? "";
                SelectedRecipe.Ingredients = Ingredients.ToList();

                _recipeService.Update(SelectedRecipe);
                Load();
                StatusMessage = "Rezept gespeichert.";
            }
            catch (Exception ex)
            {
                StatusMessage = "Fehler beim Speichern: " + ex.Message;
            }
        }

        private void DeleteRecipe()
        {
            try
            {
                if (SelectedRecipe == null) return;

                StatusMessage = "";
                _recipeService.Delete(SelectedRecipe.Id);
                Load();
                ClearForm();
                StatusMessage = "Rezept gelöscht.";
            }
            catch (Exception ex)
            {
                StatusMessage = "Fehler beim Löschen: " + ex.Message;
            }
        }

        private void AddIngredient()
        {
            if (SelectedRecipe == null) return;

            var ing = new RecipeIngredient
            {
                RecipeId = SelectedRecipe.Id,
                FoodItem = NewIngredientName.Trim(),
                Amount = NewIngredientAmount,
                Unit = NewIngredientUnit?.Trim() ?? ""
            };

            Ingredients.Add(ing);

            NewIngredientName = "";
            NewIngredientAmount = 1;
            NewIngredientUnit = "";
        }

        private void RemoveIngredient()
        {
            if (SelectedIngredient == null) return;
            Ingredients.Remove(SelectedIngredient);
            SelectedIngredient = null;
        }

        private void ClearForm()
        {
            SelectedRecipe = null;
            RecipeName = "";
            Description = "";
            Instructions = "";
            Ingredients.Clear();
            SelectedIngredient = null;

            NewIngredientName = "";
            NewIngredientAmount = 1;
            NewIngredientUnit = "";
        }
    }
}
