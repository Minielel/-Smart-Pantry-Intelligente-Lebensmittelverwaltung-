using Smartpantry.Helpers;
using Smartpantry.Models;
using SmartPantry2.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace Smartpantry.ViewModels
{
    public class ShoppingListViewModel : BaseViewModel
    {
        private readonly ShoppingService _shoppingService = new ShoppingService();

        public ObservableCollection<ShoppingItem> Items { get; } = new ObservableCollection<ShoppingItem>();

        private ShoppingItem? _selectedItem;
        public ShoppingItem? SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (SetProperty(ref _selectedItem, value))
                    MarkAsBoughtCommand.RaiseCanExecuteChanged();
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

        private string _statusMessage = "";
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

        public RelayCommand LoadCommand { get; }
        public RelayCommand AddCommand { get; }
        public RelayCommand MarkAsBoughtCommand { get; }
        public RelayCommand ClearFormCommand { get; }

        public ShoppingListViewModel()
        {
            LoadCommand = new RelayCommand(Load);
            AddCommand = new RelayCommand(Add, CanAdd);
            MarkAsBoughtCommand = new RelayCommand(MarkAsBought, () => SelectedItem != null && !SelectedItem.IsBought);
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
                Items.Clear();

                var userId = UserSession.CurrentUserId;
                if (userId == null) return;

                var list = _shoppingService.GetAll(userId.Value)
                    .OrderBy(i => i.IsBought)
                    .ThenBy(i => i.Name);

                foreach (var item in list)
                    Items.Add(item);
            }
            catch (Exception ex)
            {
                StatusMessage = "Fehler beim Laden der Einkaufsliste: " + ex.Message;
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

                var item = new ShoppingItem
                {
                    UserId = userId.Value,
                    Name = Name.Trim(),
                    Amount = Amount,
                    Unit = Unit?.Trim() ?? "",
                    IsBought = false
                };

                _shoppingService.Add(item);
                Load();
                ClearForm();
                StatusMessage = "Einkaufsitem hinzugefügt.";
            }
            catch (Exception ex)
            {
                StatusMessage = "Fehler beim Hinzufügen: " + ex.Message;
            }
        }

        private void MarkAsBought()
        {
            try
            {
                if (SelectedItem == null) return;

                StatusMessage = "";
                _shoppingService.MarkAsBought(SelectedItem.Id);
                Load();
                StatusMessage = "Als gekauft markiert.";
            }
            catch (Exception ex)
            {
                StatusMessage = "Fehler beim Markieren: " + ex.Message;
            }
        }

        private void ClearForm()
        {
            Name = "";
            Amount = 1;
            Unit = "";
            SelectedItem = null;
        }
    }
}
