// ------------------------------------------------------------
// Datei: FoodViewModel.cs
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
using System.Collections.ObjectModel;
using System.Linq;
using System.Collections.Generic;

namespace Smartpantry.ViewModels
{



    public class FoodViewModel : BaseViewModel
    {
        // Dieses ViewModel verwaltet den Vorratsbereich.
        // Hier werden Lebensmittel geladen, neu angelegt und geloescht.
        private readonly FoodService _foodService = new FoodService();



        public ObservableCollection<FoodItem> FoodItems { get; } = new ObservableCollection<FoodItem>();


        public bool CanEdit => UserSession.IsAdmin;



        public ObservableCollection<string> Units { get; } = new ObservableCollection<string>(
            new List<string> { "g", "ml", "Stück" });

        private FoodItem? _selectedFoodItem;


        public FoodItem? SelectedFoodItem
        {
            get => _selectedFoodItem;
            set
            {
                if (SetProperty(ref _selectedFoodItem, value))
                {
                    if (value != null)
                    {
                        Name = value.Name;
                        Amount = value.Amount;
                        Unit = value.Unit;
                        SelectedUnit = string.IsNullOrWhiteSpace(value.Unit) ? "g" : value.Unit;
                        ExpiryDate = value.ExpiryDate;
                        CategoryId = value.CategoryId;

                        if (IsSelectionMode)
                        {
                            FoodChosen?.Invoke(value);
                            return;
                        }
                    }

                    UpdateCommand.RaiseCanExecuteChanged();
                    DeleteCommand.RaiseCanExecuteChanged();
                }
            }
        }

        private string _name = "";
        public string Name
        {
            get => _name;
            set
            {
                if (SetProperty(ref _name, value))
                    AddCommand.RaiseCanExecuteChanged();
            }
        }

        private decimal _amount = 1;
        public decimal Amount { get => _amount; set => SetProperty(ref _amount, value); }

        private string _unit = "";
        public string Unit { get => _unit; set => SetProperty(ref _unit, value); }

        private string _selectedUnit = "g";
        public string SelectedUnit
        {
            get => _selectedUnit;
            set
            {
                if (SetProperty(ref _selectedUnit, value))
                {

                    Unit = value ?? "";
                }
            }
        }


        private bool _isSelectionMode;
        public bool IsSelectionMode
        {
            get => _isSelectionMode;
            set => SetProperty(ref _isSelectionMode, value);
        }

        public RelayCommand<FoodItem> ChooseFoodCommand { get; }
        public event Action<FoodItem>? FoodChosen;

        private DateTime _expiryDate = DateTime.Today.AddDays(7);
        public DateTime ExpiryDate { get => _expiryDate; set => SetProperty(ref _expiryDate, value); }

        private int? _categoryId;
        public int? CategoryId { get => _categoryId; set => SetProperty(ref _categoryId, value); }

        private string _statusMessage = "";
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

        public RelayCommand LoadCommand { get; }
        public RelayCommand AddCommand { get; }
        public RelayCommand UpdateCommand { get; }
        public RelayCommand DeleteCommand { get; }
        public RelayCommand ClearFormCommand { get; }


        public RelayCommand<FoodItem> DeleteFoodItemCommand { get; }



        public FoodViewModel()
        {
            LoadCommand = new RelayCommand(Load);
            AddCommand = new RelayCommand(Add, CanAdd);
            UpdateCommand = new RelayCommand(Update, () => CanEdit && SelectedFoodItem != null);
            DeleteCommand = new RelayCommand(Delete, () => CanEdit && SelectedFoodItem != null);
            ClearFormCommand = new RelayCommand(ClearForm);

            ChooseFoodCommand = new RelayCommand<FoodItem>(ChooseFood);

            DeleteFoodItemCommand = new RelayCommand<FoodItem>(DeleteFromTile, item => CanEdit && item != null);

            UserSession.CurrentUserChanged += () =>
            {
                ClearForm();
                Load();
                OnPropertyChanged(nameof(CanEdit));
                AddCommand.RaiseCanExecuteChanged();
                UpdateCommand.RaiseCanExecuteChanged();
                DeleteCommand.RaiseCanExecuteChanged();
                DeleteFoodItemCommand.RaiseCanExecuteChanged();
            };

            FoodService.FoodChanged += () => Load();

            Load();
        }



        public void StartSelectionMode(Action<FoodItem> onChosen)
        {
            FoodChosen = null;
            FoodChosen += onChosen;
            IsSelectionMode = true;
        }

        public void EndSelectionMode()
        {
            IsSelectionMode = false;
            FoodChosen = null;
        }


        private void ChooseFood(FoodItem? item)
        {
            if (item == null) return;
            if (!IsSelectionMode) return;
            FoodChosen?.Invoke(item);
        }

        private bool CanAdd() =>
            UserSession.CurrentUserId != null && CanEdit && !string.IsNullOrWhiteSpace(Name);




        public void Load()
        {
            try
            {
                StatusMessage = "";
                FoodItems.Clear();

                var all = _foodService.GetAll();
                var userId = UserSession.CurrentUserId;

                var filtered = userId == null
                    ? Enumerable.Empty<FoodItem>()
                    : all.Where(f => f.UserId == userId.Value);

                foreach (var item in filtered.OrderBy(f => f.ExpiryDate))
                    FoodItems.Add(item);
            }
            catch (Exception ex)
            {
                StatusMessage = "Fehler beim Laden der Lebensmittel: " + ex.Message;
            }
        }




        private void Add()
        {
            // Diese Methode legt ein neues Lebensmittel an.
            // Wenn alle Eingaben gueltig sind, wird der Datensatz in der Datenbank gespeichert.
            try
            {
                StatusMessage = "";
                var userId = UserSession.CurrentUserId;
                if (userId == null)
                {
                    StatusMessage = "Bitte zuerst einloggen.";
                    return;
                }

                var item = new FoodItem
                {
                    UserId = userId.Value,
                    Name = Name.Trim(),
                    Amount = Amount,
                    Unit = (SelectedUnit ?? Unit)?.Trim() ?? "",
                    ExpiryDate = ExpiryDate,
                    CategoryId = CategoryId,
                    CreatedAt = DateTime.Now
                };

                _foodService.Add(item);
                Load();
                ClearForm();
                StatusMessage = "Lebensmittel hinzugefügt.";
            }
            catch (Exception ex)
            {
                StatusMessage = "Fehler beim Hinzufügen: " + ex.Message;
            }
        }

        private void Update()
        {
            try
            {
                if (SelectedFoodItem == null) return;

                StatusMessage = "";
                SelectedFoodItem.Name = Name.Trim();
                SelectedFoodItem.Amount = Amount;
                SelectedFoodItem.Unit = (SelectedUnit ?? Unit)?.Trim() ?? "";
                SelectedFoodItem.ExpiryDate = ExpiryDate;
                SelectedFoodItem.CategoryId = CategoryId;

                _foodService.Update(SelectedFoodItem);
                Load();
                StatusMessage = "Lebensmittel gespeichert.";
            }
            catch (Exception ex)
            {
                StatusMessage = "Fehler beim Speichern: " + ex.Message;
            }
        }

        private void Delete()
        {
            try
            {
                if (SelectedFoodItem == null) return;

                StatusMessage = "";
                _foodService.Delete(SelectedFoodItem.Id);
                Load();
                ClearForm();
                StatusMessage = "Lebensmittel gelöscht.";
            }
            catch (Exception ex)
            {
                StatusMessage = "Fehler beim Löschen: " + ex.Message;
            }
        }

        private void DeleteFromTile(FoodItem? item)
        {
            try
            {
                if (item == null) return;

                StatusMessage = "";
                _foodService.Delete(item.Id);


                if (SelectedFoodItem?.Id == item.Id)
                    ClearForm();

                Load();
                StatusMessage = "Lebensmittel gelöscht.";
            }
            catch (Exception ex)
            {
                StatusMessage = "Fehler beim Löschen: " + ex.Message;
            }
        }


        private void ClearForm()
        {
            SelectedFoodItem = null;
            Name = "";
            Amount = 1;
            SelectedUnit = "g";
            ExpiryDate = DateTime.Today.AddDays(7);
            CategoryId = null;
        }
    }
}
