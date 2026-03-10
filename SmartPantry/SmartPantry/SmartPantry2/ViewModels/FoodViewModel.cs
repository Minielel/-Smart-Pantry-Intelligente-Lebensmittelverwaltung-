// ============================================================
// Datei:   FoodViewModel.cs
// Schicht: ViewModel / Vorratsverwaltung
//
// ZWECK:
//   Verwaltet die FoodView: Liste aller Lebensmittel als Kacheln,
//   Formular zum Hinzufügen/Bearbeiten, Lösch-Funktion und
//   einen Auswahlmodus für die Rezepterstellung.
//
// ROTER FADEN:
//   FoodView.xaml ←→ FoodViewModel ←→ FoodService ←→ DB: food_items
//
//   AUSWAHLMODUS (seitenübergreifend für Rezepte):
//   MainViewModel ruft FoodVM.StartSelectionMode(callback) auf.
//   → IsSelectionMode = true → FoodView zeigt "Auswählen"-Hinweis
//   → User klickt Item in der Liste
//   → SelectedFoodItem-Setter erkennt IsSelectionMode=true
//   → _onFoodChosen(selectedItem) → Callback zu MainViewModel/RecipesVM
//   → EndSelectionMode() → IsSelectionMode = false
//
//   FoodService.FoodChanged (statisches Event):
//   → wird von FoodService nach Add/Update/Delete gefeuert
//   → FoodViewModel.Load() → Liste aktualisieren
//   → ShoppingListViewModel.Load() → Low-Stock neu prüfen
//
//   CanEdit: nur Admin-User dürfen Daten ändern
//   → steuert ob Formular sichtbar ist (BoolToVisibilityConverter)
//   → steuert ob "×"-Buttons auf Kacheln sichtbar sind
//
// USER USECASE ADMIN:
//   "Food" öffnen → alle eigenen Lebensmittel als Kacheln sehen
//   Name, Menge, Einheit, Ablaufdatum eingeben → "Hinzufügen"
//   → FoodService.Add() → Merge-Logik → DB-Eintrag
//   "×" auf Kachel → Bestätigung → DeleteFromTile() → gelöscht
//
// USER USECASE STANDARD:
//   Nur Anzeige (CanEdit = false → Formular + Lösch-Buttons unsichtbar)
//
// QUELLEN:
//   ObservableCollection<T> (automatische UI-Aktualisierung bei Listenänderungen):
//   https://learn.microsoft.com/dotnet/api/system.collections.objectmodel.observablecollection-1
//
//   WPF ItemsSource Binding:
//   https://learn.microsoft.com/dotnet/desktop/wpf/controls/itemscontrol
//
//   Action<T> Delegate (für Auswahlmodus-Callback):
//   https://learn.microsoft.com/dotnet/api/system.action-1
// ============================================================

using Smartpantry.Helpers;
using Smartpantry.Models;
using SmartPantry2.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace Smartpantry.ViewModels
{
    public class FoodViewModel : BaseViewModel
    {
        // FoodService: alle DB-Operationen für Lebensmittel
        private readonly FoodService _foodService = new FoodService();

        // ── LISTE DER LEBENSMITTEL ─────────────────────────────────────────────────
        // ObservableCollection: WPF hört automatisch auf Add/Remove/Clear-Änderungen.
        // Im Gegensatz zu List<T>: keine manuelle PropertyChanged-Benachrichtigung nötig.
        // Quelle: https://learn.microsoft.com/dotnet/api/system.collections.objectmodel.observablecollection-1
        private ObservableCollection<FoodItem> _foodItems = new();
        public ObservableCollection<FoodItem> FoodItems
        {
            get => _foodItems;
            // Setter: wird aufgerufen wenn Load() die Liste komplett ersetzt
            private set => SetProperty(ref _foodItems, value);
        }

        // ── FORMULAR-FELDER (Bindings für Texteingaben) ────────────────────────────
        // Alle Felder steuern auch ob der "Hinzufügen"-Button aktiv ist

        private string _name = "";
        // Name des Lebensmittels (z.B. "Milch") → DB-Spalte "name"
        public string Name
        {
            get => _name;
            set
            {
                if (SetProperty(ref _name, value))
                    // Name ist Pflichtfeld → Button-Zustand neu prüfen
                    AddCommand.RaiseCanExecuteChanged();
            }
        }

        private decimal _amount;
        // Menge (z.B. 500) → DB-Spalte "amount" (DECIMAL(10,2))
        public decimal Amount
        {
            get => _amount;
            set => SetProperty(ref _amount, value);
        }

        private string _unit = "g";
        // Einheit (z.B. "g", "ml", "Stück") → DB-Spalte "unit"
        public string Unit
        {
            get => _unit;
            set => SetProperty(ref _unit, value);
        }

        private DateTime _expiryDate = DateTime.Today.AddDays(7);
        // Ablaufdatum → DB-Spalte "expiration_date"
        // ACHTUNG: C# "ExpiryDate" ↔ DB "expiration_date" (Mapping in FoodDbContext!)
        // Standardwert: eine Woche ab heute (sinnvoller Startwert im DatePicker)
        public DateTime ExpiryDate
        {
            get => _expiryDate;
            set => SetProperty(ref _expiryDate, value);
        }

        // ── AUSGEWÄHLTES ITEM (für Bearbeiten + Auswahlmodus) ─────────────────────
        private FoodItem? _selectedFoodItem;
        public FoodItem? SelectedFoodItem
        {
            get => _selectedFoodItem;
            set
            {
                if (SetProperty(ref _selectedFoodItem, value))
                {
                    // Wenn Auswahlmodus aktiv: direkt den Callback aufrufen
                    if (IsSelectionMode && value != null)
                    {
                        // _onFoodChosen wurde von StartSelectionMode gesetzt
                        // Übergibt das gewählte Item an MainViewModel → RecipesVM
                        _onFoodChosen?.Invoke(value);
                    }
                }
            }
        }

        // ── AUSWAHLMODUS ──────────────────────────────────────────────────────────
        // Wird von MainViewModel aktiviert wenn RecipesVM eine Zutat auswählen will.

        // true = Auswahlmodus aktiv → FoodView zeigt Auswahlhinweis
        // false = normaler Modus → Formular ist sichtbar
        private bool _isSelectionMode;
        public bool IsSelectionMode
        {
            get => _isSelectionMode;
            private set => SetProperty(ref _isSelectionMode, value);
        }

        // Privater Callback: wird aufgerufen wenn User ein Item auswählt
        // "?" = nullable: null wenn kein Auswahlmodus aktiv
        private Action<FoodItem>? _onFoodChosen;

        // ── BERECHTIGUNGEN ────────────────────────────────────────────────────────
        // Steuert Sichtbarkeit von Formular und Lösch-Buttons
        // Wird nach Login/Logout neu berechnet
        public bool CanEdit => UserSession.IsAdmin;

        // ── STATUS-MELDUNG ────────────────────────────────────────────────────────
        private string _statusMessage = "";
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        // ── COMMANDS ──────────────────────────────────────────────────────────────
        // RelayCommand (ohne Parameter): für Aktionen ohne spezifisches Objekt
        // RelayCommand<T> (mit Parameter): für Aktionen auf einem konkreten Item

        // "Hinzufügen"-Button: Add neues FoodItem
        public RelayCommand AddCommand { get; }

        // "×"-Button auf jeder Kachel: löscht das jeweilige FoodItem
        // RelayCommand<FoodItem>: die Kachel übergibt sich selbst als Parameter
        public RelayCommand<FoodItem> DeleteFoodItemCommand { get; }

        public FoodViewModel()
        {
            // Add: aktiv wenn Name nicht leer UND User eingeloggt UND Admin
            AddCommand = new RelayCommand(Add, CanAdd);

            // DeleteFromTile: aktiv wenn CanEdit=true UND item nicht null
            DeleteFoodItemCommand = new RelayCommand<FoodItem>(
                DeleteFromTile,
                item => CanEdit && item != null);

            // Bei Login/Logout: Liste neu laden + Berechtigungen aktualisieren
            UserSession.CurrentUserChanged += () =>
            {
                // CanEdit basiert auf IsAdmin → neu berechnen
                OnPropertyChanged(nameof(CanEdit));
                // Add/Delete Commands neu prüfen
                AddCommand.RaiseCanExecuteChanged();
                DeleteFoodItemCommand.RaiseCanExecuteChanged();
                // Liste für neuen User laden (oder leeren bei Logout)
                Load();
            };

            // FoodService feuert FoodChanged nach jeder DB-Änderung:
            // → auch wenn ShoppingService ein Item in food_items legt
            // → Liste bleibt immer aktuell ohne manuellen Refresh
            FoodService.FoodChanged += Load;

            // Erste Ladung beim Erstellen des ViewModels
            Load();
        }

        // --------------------------------------------------------
        // Load
        //
        // FUNKTION:
        //   Lädt alle FoodItems des aktuell eingeloggten Users aus DB.
        //   Filtert nach UserId (jeder User sieht nur seine Items).
        //   Ersetzt die ObservableCollection komplett.
        //
        // AUFGERUFEN VON:
        //   Konstruktor (einmalig beim Start)
        //   UserSession.CurrentUserChanged (bei Login/Logout)
        //   FoodService.FoodChanged (nach jeder Vorrats-Änderung)
        //
        // DB-Zugriff:
        //   FoodService.GetAll() → SELECT * FROM food_items LEFT JOIN categories
        //   → dann clientseitiger Filter nach UserId
        // --------------------------------------------------------
        private void Load()
        {
            // Wenn niemand eingeloggt → Liste leeren und abbrechen
            if (UserSession.CurrentUserId == null)
            {
                FoodItems = new ObservableCollection<FoodItem>();
                return;
            }

            try
            {
                var userId = UserSession.CurrentUserId.Value;

                // FoodService.GetAll(): alle Items aus DB (alle User!)
                // .Where() filtert dann clientseitig auf den aktuellen User
                // .OrderBy() nach Ablaufdatum: bald ablaufende Items zuerst
                var items = _foodService.GetAll()
                    .Where(f => f.UserId == userId)
                    .OrderBy(f => f.ExpiryDate)
                    .ToList();

                // ObservableCollection neu befüllen:
                // new ObservableCollection<>(items) erstellt aus List<> eine Collection
                // → WPF erkennt die Änderung per SetProperty und zeichnet die Liste neu
                FoodItems = new ObservableCollection<FoodItem>(items);
            }
            catch (Exception ex)
            {
                StatusMessage = "Fehler beim Laden: " + ex.Message;
            }
        }

        // --------------------------------------------------------
        // CanAdd
        //
        // RETURN: true wenn "Hinzufügen"-Button aktiv sein soll
        //
        // BEDINGUNGEN:
        //   - User ist eingeloggt (CurrentUserId != null)
        //   - User ist Admin (CanEdit = true)
        //   - Name-Feld ist nicht leer
        // --------------------------------------------------------
        private bool CanAdd() =>
            UserSession.CurrentUserId != null &&
            CanEdit &&
            !string.IsNullOrWhiteSpace(Name);

        // --------------------------------------------------------
        // Add
        //
        // FUNKTION:
        //   Erstellt neues FoodItem-Objekt und übergibt es an FoodService.
        //   FoodService.Add() hat Merge-Logik: gleicher Name+Einheit+Datum
        //   → Menge aufaddieren statt neuen Datensatz anlegen.
        //
        // NACH ABSCHLUSS:
        //   FoodService.RaiseFoodChanged() → Load() → Liste aktualisiert
        //   Formular wird geleert für nächste Eingabe.
        //
        // AUFGERUFEN VON: AddCommand (Button-Klick in FoodView)
        // --------------------------------------------------------
        private void Add()
        {
            try
            {
                // Neues FoodItem aus Formular-Daten zusammenbauen
                var item = new FoodItem
                {
                    // UserId aus der aktuellen Session
                    UserId     = UserSession.CurrentUserId!.Value,
                    // .Trim(): Leerzeichen am Anfang/Ende entfernen
                    Name       = Name.Trim(),
                    Amount     = Amount,
                    Unit       = Unit.Trim(),
                    // .Date: nur Datum, keine Uhrzeit → konsistent mit DB DATE-Typ
                    ExpiryDate = ExpiryDate.Date,
                    // Erstellungszeitpunkt jetzt setzen
                    CreatedAt  = DateTime.Now
                };

                // FoodService.Add() übernimmt Merge-Logik und DB-Schreiben
                // → danach RaiseFoodChanged() → Load() wird automatisch aufgerufen
                _foodService.Add(item);

                // Formular leeren für nächste Eingabe
                ResetForm();
                StatusMessage = "";
            }
            catch (Exception ex)
            {
                StatusMessage = "Fehler beim Hinzufügen: " + ex.Message;
            }
        }

        // --------------------------------------------------------
        // DeleteFromTile
        //
        // FUNKTION:
        //   Löscht ein FoodItem das über den "×"-Button einer Kachel
        //   übergeben wird. Fragt vorher per MessageBox nach Bestätigung.
        //
        // PARAMETER:
        //   item → das FoodItem das gelöscht werden soll
        //          (wird von RelayCommand<FoodItem> aus CommandParameter übernommen)
        //
        // AUFGERUFEN VON: DeleteFoodItemCommand (RelayCommand<FoodItem>)
        //   In FoodView.xaml:
        //     Command="{Binding DataContext.DeleteFoodItemCommand,
        //               RelativeSource={RelativeSource AncestorType=ListBox}}"
        //     CommandParameter="{Binding}"
        // --------------------------------------------------------
        private void DeleteFromTile(FoodItem? item)
        {
            // null-Check: defensiv falls Command mit null aufgerufen wird
            if (item == null) return;

            // MessageBox: Sicherheitsabfrage bevor gelöscht wird
            // MessageBoxButton.YesNo: Ja/Nein-Dialog
            // MessageBoxImage.Warning: Warnsymbol
            // Quelle: https://learn.microsoft.com/dotnet/api/system.windows.messagebox
            var result = MessageBox.Show(
                $"'{item.Name}' wirklich löschen?",
                "Löschen bestätigen",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            // Nur löschen wenn User "Ja" geklickt hat
            if (result != MessageBoxResult.Yes) return;

            try
            {
                // FoodService.Delete() → DELETE FROM food_items WHERE id=?
                // → danach RaiseFoodChanged() → Load() wird aufgerufen
                _foodService.Delete(item.Id);
            }
            catch (Exception ex)
            {
                StatusMessage = "Fehler beim Löschen: " + ex.Message;
            }
        }

        // --------------------------------------------------------
        // StartSelectionMode
        //
        // FUNKTION:
        //   Aktiviert den Auswahlmodus. Wird von MainViewModel aufgerufen
        //   wenn RecipesVM eine Zutat auswählen lassen möchte.
        //
        // PARAMETER:
        //   onChosen → Callback-Methode die aufgerufen wird wenn User ein Item wählt
        //              Typ: Action<FoodItem> (bekommt das gewählte Item)
        //
        // WIRKUNG:
        //   IsSelectionMode = true
        //   → FoodView.xaml kann per DataTrigger einen Hinweistext einblenden
        //   → SelectedFoodItem-Setter ruft onChosen auf wenn Item gewählt wird
        // --------------------------------------------------------
        public void StartSelectionMode(Action<FoodItem> onChosen)
        {
            // Callback für spätere Verwendung speichern
            _onFoodChosen   = onChosen;
            // Modus aktivieren → UI-Feedback für User
            IsSelectionMode = true;
        }

        // --------------------------------------------------------
        // EndSelectionMode
        //
        // FUNKTION:
        //   Beendet den Auswahlmodus und räumt auf.
        //   Wird von MainViewModel aufgerufen nachdem die Auswahl verarbeitet wurde.
        // --------------------------------------------------------
        public void EndSelectionMode()
        {
            // Callback löschen (kein versehentlicher Aufruf mehr möglich)
            _onFoodChosen   = null;
            // Modus deaktivieren → normale FoodView
            IsSelectionMode = false;
            // Selektion zurücksetzen
            SelectedFoodItem = null;
        }

        // --------------------------------------------------------
        // ResetForm
        //
        // FUNKTION: leert alle Formular-Felder nach erfolgreichem Add
        // --------------------------------------------------------
        private void ResetForm()
        {
            Name       = "";
            Amount     = 0;
            Unit       = "g";
            ExpiryDate = DateTime.Today.AddDays(7);
        }
    }
}