// ------------------------------------------------------------
// Datei: ShoppingListViewModel.cs
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

namespace Smartpantry.ViewModels
{


    public class ShoppingListViewModel : BaseViewModel
    {
        // Dieses ViewModel ist fuer die Einkaufsliste zustaendig.
        // Es sammelt Eintraege, uebernimmt sie spaeter in den Vorrat und aktualisiert die Anzeige.
        private readonly ShoppingService _shoppingService = new ShoppingService();
        private readonly FoodService _foodService = new FoodService();


        public ObservableCollection<ShoppingItem> Items { get; } = new ObservableCollection<ShoppingItem>();


        public bool CanEdit => UserSession.IsAdmin;

        private ShoppingItem? _selectedItem;
        public ShoppingItem? SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (SetProperty(ref _selectedItem, value))
                {
                    if (value != null)
                        MoveToFoodExpiryDate = DateTime.Today.AddDays(7);
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

        private string _statusMessage = "";
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }



        private DateTime _moveToFoodExpiryDate = DateTime.Today.AddDays(7);
        public DateTime MoveToFoodExpiryDate
        {
            get => _moveToFoodExpiryDate;
            set => SetProperty(ref _moveToFoodExpiryDate, value);
        }

        public RelayCommand LoadCommand { get; }
        public RelayCommand AddCommand { get; }
        public RelayCommand<ShoppingItem> DeleteItemCommand { get; }
        public RelayCommand<ShoppingItem> AddToFoodCommand { get; }

        public ObservableCollection<string> Units { get; } = new ObservableCollection<string>
        {
            "g",
            "ml",
            "Stück"
        };

        private string _selectedUnit = "g";
        public string SelectedUnit
        {
            get => _selectedUnit;
            set
            {
                if (SetProperty(ref _selectedUnit, value))
                    Unit = value;
            }
        }




        public ShoppingListViewModel()
        {
            LoadCommand = new RelayCommand(Load);
            AddCommand = new RelayCommand(Add, CanAdd);
            DeleteItemCommand = new RelayCommand<ShoppingItem>(DeleteItem, item => CanEdit && item != null);
            AddToFoodCommand = new RelayCommand<ShoppingItem>(AddToFood, item => CanEdit && item != null);

            UserSession.CurrentUserChanged += () =>
            {
                ClearForm();
                Load();
                OnPropertyChanged(nameof(CanEdit));
                AddCommand.RaiseCanExecuteChanged();
                DeleteItemCommand.RaiseCanExecuteChanged();
                AddToFoodCommand.RaiseCanExecuteChanged();
            };


            FoodService.FoodChanged += () => Load();


            Unit = SelectedUnit;
            Load();
        }

        private bool CanAdd() =>
            UserSession.CurrentUserId != null && CanEdit && !string.IsNullOrWhiteSpace(Name);




        public void Load()
        {
            try
            {
                StatusMessage = "";
                Items.Clear();

                var userId = UserSession.CurrentUserId;
                if (userId == null) return;




                _shoppingService.UpsertLowStockFromFood(userId.Value);

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
            // Hier wird ein neuer Eintrag in die Einkaufsliste uebernommen.
            // Danach wird die Liste neu geladen, damit man den Eintrag sofort sieht.
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
                    Unit = (SelectedUnit ?? Unit)?.Trim() ?? "",
                    IsBought = false
                };

                _shoppingService.AddOrMerge(item);
                Load();
                ClearForm();
                StatusMessage = "Einkaufsitem hinzugefügt.";
                MoveToFoodExpiryDate = DateTime.Today.AddDays(7);
            }
            catch (Exception ex)
            {
                StatusMessage = "Fehler beim Hinzufügen: " + ex.Message;
            }
        }



        private void DeleteItem(ShoppingItem? item)
        {
            try
            {
                if (item == null) return;
                StatusMessage = "";

                _shoppingService.Delete(item.Id);
                Load();

                if (SelectedItem?.Id == item.Id)
                    SelectedItem = null;

                StatusMessage = "Eintrag gelöscht.";
            }
            catch (Exception ex)
            {
                StatusMessage = "Fehler beim Löschen: " + ex.Message;
            }
        }

        private void ClearForm()
        {
            Name = "";
            Amount = 1;
            SelectedUnit = Units.FirstOrDefault() ?? "g";
            Unit = SelectedUnit;
            SelectedItem = null;
        }




        private void AddToFood(ShoppingItem? item)
        {
            try
            {
                if (item == null) return;
                var userId = UserSession.CurrentUserId;
                if (userId == null) return;

                StatusMessage = "";
                _shoppingService.MoveToFoodAndRemove(userId.Value, item.Id, MoveToFoodExpiryDate.Date);
                Load();
                SelectedItem = null;
                StatusMessage = "Zu Food hinzugefügt.";
            }
            catch (Exception ex)
            {
                StatusMessage = "Fehler beim Übernehmen: " + ex.Message;
            }
        }
    }
}
