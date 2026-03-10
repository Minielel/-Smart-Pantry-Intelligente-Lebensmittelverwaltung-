// ============================================================
// Datei:   ShoppingListViewModel.cs
// Schicht: ViewModel / Einkaufsliste
//
// ZWECK:
//   Verwaltet die Einkaufsliste. Ruft bei jedem Load() automatisch
//   die Low-Stock-Erkennung auf (UpsertLowStockFromFood).
//   Ermöglicht den Transfer von eingekauften Items in den Vorrat.
//
// ROTER FADEN:
//   ShoppingListView.xaml ←→ ShoppingListViewModel
//   ←→ ShoppingService ←→ DB: shopping_list + food_items
//
//   AUTOMATISCHE LOW-STOCK-ERKENNUNG:
//   Bei jedem Load() → ShoppingService.UpsertLowStockFromFood(userId)
//   → Vorrat wird analysiert (Schwellenwerte je nach Einheit)
//   → Items mit niedrigem Bestand die noch nicht auf der Liste sind
//     werden automatisch hinzugefügt
//
//   "ZU FOOD" WORKFLOW:
//   User wählt ExpiryDate (DatePicker in Seitenleiste)
//   User klickt "Zu Food" Checkbox bei einem Shopping-Item
//   → AddToFoodCommand(item) → ShoppingService.MoveToFoodAndRemove()
//   → Transaktion: Item → food_items, Item aus shopping_list gelöscht
//   → FoodService.RaiseFoodChanged() → FoodView + Dashboard aktualisieren
//
//   FoodService.FoodChanged → Load()
//   → wenn Vorrat sich ändert, Low-Stock neu prüfen
//
// USER USECASE:
//   Admin öffnet Einkaufsliste
//   → automatisch: Artikel mit niedrigem Bestand erscheinen als Vorschläge
//   → manuell: Name + Menge + Einheit eingeben → "Hinzufügen"
//   → nach dem Einkauf: Ablaufdatum im DatePicker wählen
//   → "Zu Food" Checkbox → Item landet im Vorrat
//
// QUELLEN:
//   ObservableCollection<T>:
//   https://learn.microsoft.com/dotnet/api/system.collections.objectmodel.observablecollection-1
//
//   EF Core Transactions (in ShoppingService.MoveToFoodAndRemove):
//   https://learn.microsoft.com/ef/core/saving/transactions
//
//   RelayCommand<T> (typisierter Command für Shopping-Items):
//   Siehe RelayCommandT.cs in diesem Projekt
// ============================================================

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
        // ShoppingService: alle DB-Operationen für die Einkaufsliste
        private readonly ShoppingService _shoppingService = new ShoppingService();

        // ── EINKAUFSLISTE ─────────────────────────────────────────────────────────
        // ObservableCollection: WPF reagiert sofort auf Add/Remove
        private ObservableCollection<ShoppingItem> _items = new();
        public ObservableCollection<ShoppingItem> Items
        {
            get => _items;
            private set => SetProperty(ref _items, value);
        }

        // ── ABLAUFDATUM FÜR "ZU FOOD" TRANSFER ───────────────────────────────────
        // User wählt dieses Datum per DatePicker bevor er "Zu Food" klickt.
        // Wird als ExpiryDate des neuen FoodItems verwendet.
        private DateTime _expiryDateForFood = DateTime.Today.AddDays(7);
        public DateTime ExpiryDateForFood
        {
            get => _expiryDateForFood;
            set => SetProperty(ref _expiryDateForFood, value);
        }

        // ── FORMULAR-FELDER (manuelle Eingabe) ────────────────────────────────────
        private string _newName = "";
        // Name des einzukaufenden Artikels → DB-Spalte "name"
        public string NewName
        {
            get => _newName;
            set
            {
                if (SetProperty(ref _newName, value))
                    AddCommand.RaiseCanExecuteChanged();
            }
        }

        private decimal _newAmount;
        // Menge → DB-Spalte "amount"
        public decimal NewAmount
        {
            get => _newAmount;
            set => SetProperty(ref _newAmount, value);
        }

        private string _newUnit = "g";
        // Einheit → DB-Spalte "unit"
        public string NewUnit
        {
            get => _newUnit;
            set => SetProperty(ref _newUnit, value);
        }

        // ── BERECHTIGUNGEN ────────────────────────────────────────────────────────
        // Nur Admins dürfen hinzufügen, löschen und zu Food übernehmen
        public bool CanEdit => UserSession.IsAdmin;

        private string _statusMessage = "";
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        // ── COMMANDS ──────────────────────────────────────────────────────────────
        // Manuelles Hinzufügen eines Shopping-Items
        public RelayCommand AddCommand { get; }

        // "Zu Food" Checkbox: überträgt Item in den Vorrat
        // RelayCommand<ShoppingItem>: das konkrete Item wird als Parameter übergeben
        public RelayCommand<ShoppingItem> AddToFoodCommand { get; }

        // "×"-Button: löscht ein Shopping-Item dauerhaft
        public RelayCommand<ShoppingItem> DeleteItemCommand { get; }

        public ShoppingListViewModel()
        {
            AddCommand = new RelayCommand(Add, () => CanEdit && !string.IsNullOrWhiteSpace(NewName));

            AddToFoodCommand = new RelayCommand<ShoppingItem>(
                AddToFood,
                // Aktiv wenn Admin UND Item nicht null UND noch nicht übernommen
                item => CanEdit && item != null && !item.IsBought);

            DeleteItemCommand = new RelayCommand<ShoppingItem>(
                DeleteItem,
                item => CanEdit && item != null);

            // Bei Login/Logout neu laden
            UserSession.CurrentUserChanged += () =>
            {
                OnPropertyChanged(nameof(CanEdit));
                AddCommand.RaiseCanExecuteChanged();
                Load();
            };

            // FoodService.FoodChanged → Low-Stock neu prüfen + Liste neu laden
            // Wenn jemand im FoodView ein Item löscht → könnte Low-Stock entstehen
            FoodService.FoodChanged += Load;

            Load();
        }

        // --------------------------------------------------------
        // Load
        //
        // FUNKTION:
        //   1. UpsertLowStockFromFood(): Vorrat analysieren, Vorschläge erstellen
        //   2. Alle Shopping-Items aus DB laden und anzeigen
        //
        // AUFGERUFEN VON:
        //   Konstruktor (einmalig beim Start)
        //   UserSession.CurrentUserChanged (bei Login/Logout)
        //   FoodService.FoodChanged (nach jeder Vorrats-Änderung)
        // --------------------------------------------------------
        public void Load()
        {
            if (UserSession.CurrentUserId == null)
            {
                Items = new ObservableCollection<ShoppingItem>();
                return;
            }

            try
            {
                var userId = UserSession.CurrentUserId.Value;

                // Schritt 1: Vorrat analysieren → Niedrig-Bestand-Items automatisch hinzufügen
                // UpsertLowStockFromFood schreibt direkt in die DB (kein Duplikat wenn schon drauf)
                _shoppingService.UpsertLowStockFromFood(userId);

                // Schritt 2: aktuelle Einkaufsliste aus DB laden
                // Nur nicht-gekaufte Items anzeigen (IsBought=false)
                // (Gekaufte Items wurden bereits zu Food übernommen und gelöscht)
                var allItems = _shoppingService.GetAll(userId)
                    .Where(s => !s.IsBought)
                    .OrderBy(s => s.Name)
                    .ToList();

                Items = new ObservableCollection<ShoppingItem>(allItems);
            }
            catch (Exception ex)
            {
                StatusMessage = "Fehler beim Laden: " + ex.Message;
            }
        }

        // --------------------------------------------------------
        // Add
        //
        // FUNKTION:
        //   Fügt manuell ein neues Shopping-Item hinzu.
        //   ShoppingService.AddOrMerge() verhindert Duplikate.
        //
        // DB: INSERT INTO shopping_list (...) ODER UPDATE ... SET amount+=?
        // --------------------------------------------------------
        private void Add()
        {
            try
            {
                var item = new ShoppingItem
                {
                    UserId   = UserSession.CurrentUserId!.Value,
                    Name     = NewName.Trim(),
                    Amount   = NewAmount,
                    Unit     = NewUnit.Trim(),
                    IsBought = false  // noch nicht eingekauft
                };

                // AddOrMerge: wenn Name+Einheit schon auf der Liste → Menge aufaddieren
                _shoppingService.AddOrMerge(item);

                // Formular leeren
                NewName   = "";
                NewAmount = 0;
                NewUnit   = "g";

                // Liste aktualisieren
                Load();
            }
            catch (Exception ex)
            {
                StatusMessage = "Fehler beim Hinzufügen: " + ex.Message;
            }
        }

        // --------------------------------------------------------
        // AddToFood
        //
        // FUNKTION:
        //   Kernfunktion: überträgt ein eingekauftes Item in den Vorrat.
        //   ExpiryDateForFood (aus DatePicker) wird als Ablaufdatum verwendet.
        //
        // FLOW:
        //   ShoppingService.MoveToFoodAndRemove()
        //   → INSERT INTO food_items (..., expiration_date=ExpiryDateForFood)
        //      ODER UPDATE food_items SET amount += ? (wenn schon vorhanden)
        //   → DELETE FROM shopping_list WHERE id=?
        //   → COMMIT (Transaktion: alles oder nichts)
        //   → FoodService.RaiseFoodChanged()
        //     → FoodView + DashboardView aktualisieren
        //     → ShoppingListView neu laden (dieses Item verschwindet)
        //
        // AUFGERUFEN VON: AddToFoodCommand (RelayCommand<ShoppingItem>)
        //   In ShoppingListView.xaml:
        //     Command="{Binding DataContext.AddToFoodCommand,
        //               RelativeSource={RelativeSource AncestorType=ListBox}}"
        //     CommandParameter="{Binding}"
        // --------------------------------------------------------
        private void AddToFood(ShoppingItem? item)
        {
            if (item == null) return;

            try
            {
                // ShoppingService übernimmt den kompletten Transfer inkl. Transaktion
                // ExpiryDateForFood: das Ablaufdatum das User im DatePicker gewählt hat
                _shoppingService.MoveToFoodAndRemove(
                    UserSession.CurrentUserId!.Value,
                    item.Id,
                    ExpiryDateForFood);

                // FoodService.RaiseFoodChanged() → Load() wird automatisch aufgerufen
                // (kein manuelles Load() nötig, da FoodChanged abonniert)
            }
            catch (Exception ex)
            {
                StatusMessage = "Fehler beim Transfer: " + ex.Message;
            }
        }

        // --------------------------------------------------------
        // DeleteItem
        //
        // FUNKTION: löscht ein Shopping-Item dauerhaft aus der Liste
        //
        // DB: DELETE FROM shopping_list WHERE id=?
        // --------------------------------------------------------
        private void DeleteItem(ShoppingItem? item)
        {
            if (item == null) return;

            try
            {
                // ShoppingService.Delete() → DELETE FROM shopping_list WHERE id=?
                _shoppingService.Delete(item.Id);
                Load();  // Liste aktualisieren
            }
            catch (Exception ex)
            {
                StatusMessage = "Fehler beim Löschen: " + ex.Message;
            }
        }
    }
}