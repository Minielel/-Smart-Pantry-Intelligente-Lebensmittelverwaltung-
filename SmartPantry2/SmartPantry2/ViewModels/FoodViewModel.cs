using Smartpantry.Helpers;
using Smartpantry.Models;
using SmartPantry2.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace Smartpantry.ViewModels
{
    public class FoodViewModel : BaseViewModel
    {
        private readonly FoodService _foodService = new FoodService();

        public ObservableCollection<FoodItem> FoodItems { get; } = new ObservableCollection<FoodItem>();

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
                        ExpiryDate = value.ExpiryDate;
                        CategoryId = value.CategoryId;
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

        public FoodViewModel()
        {
            LoadCommand = new RelayCommand(Load);
            AddCommand = new RelayCommand(Add, CanAdd);
            UpdateCommand = new RelayCommand(Update, () => SelectedFoodItem != null);
            DeleteCommand = new RelayCommand(Delete, () => SelectedFoodItem != null);
            ClearFormCommand = new RelayCommand(ClearForm);

            UserSession.CurrentUserChanged += () =>
            {
                ClearForm();
                Load();
                AddCommand.RaiseCanExecuteChanged();
            };

            Load();
        }

        private bool CanAdd() =>
            UserSession.CurrentUserId != null && !string.IsNullOrWhiteSpace(Name);

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
                    Unit = Unit?.Trim() ?? "",
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
                SelectedFoodItem.Unit = Unit?.Trim() ?? "";
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

        private void ClearForm()
        {
            SelectedFoodItem = null;
            Name = "";
            Amount = 1;
            Unit = "";
            ExpiryDate = DateTime.Today.AddDays(7);
            CategoryId = null;
        }
    }
}
