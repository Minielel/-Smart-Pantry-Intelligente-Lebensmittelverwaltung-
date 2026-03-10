// ============================================================
// Datei:   MainViewModel.cs
// Schicht: ViewModel / Zentrale Navigation
//
// ZWECK:
//   Herzstück der App. Verwaltet alle Sub-ViewModels,
//   steuert die Navigation zwischen den Seiten und reagiert
//   auf Login/Logout-Events über UserSession.
//
// ROTER FADEN:
//   MainWindow.DataContext = new MainViewModel()
//   → MainWindow.xaml hat ein ContentControl gebunden an CurrentViewModel
//   → App.xaml hat DataTemplates: wenn CurrentViewModel ein FoodViewModel ist
//     → zeige FoodView; wenn LoginViewModel → zeige LoginView usw.
//   → NavigateXxxCommand setzt CurrentViewModel → andere Seite erscheint
//
//   SEITENÜBERGREIFENDE INTERAKTION (Auswahlmodus):
//   RecipesVM braucht einen Zutaten-Namen aus dem Vorrat:
//     RecipesVM.RequestPickFood feuert
//     → MainViewModel wechselt zu FoodVM im Auswahlmodus
//     → User klickt FoodItem → Name geht zurück zu RecipesVM
//     → MainViewModel navigiert zurück zu RecipesVM
//
//   MealPlanVM braucht ein Rezept-Objekt:
//     MealPlanVM.RequestPickRecipe feuert
//     → MainViewModel wechselt zu RecipesVM im Auswahlmodus
//     → User klickt Rezept → Recipe-Objekt geht zurück zu MealPlanVM
//
// USER USECASE:
//   Jeder Klick auf Sidebar-Button (Dashboard, Food, Rezepte, Plan, Einkauf, Settings)
//   landet in einem NavigateXxxCommand hier.
//   Login/Logout wird zentral hier verarbeitet.
//
// QUELLEN:
//   WPF DataTemplate (für ViewModel→View-Mapping in App.xaml):
//   https://learn.microsoft.com/dotnet/desktop/wpf/data/data-templating-overview
//
//   MVVM Navigation Pattern:
//   https://learn.microsoft.com/dotnet/architecture/maui/mvvm
//
//   C# Events als Kommunikationskanal zwischen ViewModels:
//   https://learn.microsoft.com/dotnet/csharp/programming-guide/events/
// ============================================================

using Smartpantry.Helpers;
using Smartpantry.Models;
using SmartPantry2.Services;
using System;

namespace Smartpantry.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        // ── SUB-VIEWMODELS ─────────────────────────────────────────────────────────
        // Alle Sub-ViewModels werden einmalig beim App-Start erstellt.
        // Sie leben für die gesamte App-Laufzeit → kein Datenverlust beim Seitenwechsel.
        // Quelle MVVM Lebenszyklus: https://learn.microsoft.com/dotnet/architecture/maui/mvvm

        // Zuständig für: Login + Registrierung
        public LoginViewModel LoginVM { get; }

        // Zuständig für: Statistik-Kacheln (TotalFoodItems, ExpiringSoon usw.)
        public DashboardViewModel DashboardVM { get; }

        // Zuständig für: Vorratsliste + Formular + Auswahlmodus für Zutaten
        public FoodViewModel FoodVM { get; }

        // Zuständig für: Rezeptliste + Detailformular + Auswahlmodus für Wochenplan
        public RecipesViewModel RecipesVM { get; }

        // Zuständig für: Wochenplan-Tabelle + Rezept-/Mahlzeitauswahl
        public MealPlanViewModel MealPlanVM { get; }

        // Zuständig für: Einkaufsliste + Low-Stock-Erkennung + Food-Transfer
        public ShoppingListViewModel ShoppingVM { get; }

        // Zuständig für: Theme, Sprache, Profil, Passwort
        public SettingsViewModel SettingsVM { get; }

        // ── AKTUELL ANGEZEIGTE SEITE ───────────────────────────────────────────────
        // ContentControl in MainWindow.xaml bindet hierauf:
        //   <ContentControl Content="{Binding CurrentViewModel}"/>
        // WPF wählt die passende View per DataTemplate in App.xaml:
        //   <DataTemplate DataType="{x:Type vm:FoodViewModel}">
        //       <views:FoodView/>
        //   </DataTemplate>
        // Quelle: https://learn.microsoft.com/dotnet/desktop/wpf/data/data-templating-overview
        private BaseViewModel _currentViewModel;
        public BaseViewModel CurrentViewModel
        {
            // Getter: gibt aktuelle Seite zurück
            get => _currentViewModel;
            // Setter: privat – nur MainViewModel darf die Seite wechseln
            // SetProperty: benachrichtigt WPF → ContentControl zeigt neue View
            private set => SetProperty(ref _currentViewModel, value);
        }

        // ── NAVIGATIONSBEFEHLE (Sidebar-Buttons) ──────────────────────────────────
        // Jeder Sidebar-Button ist mit einem dieser Commands verbunden:
        //   <Button Command="{Binding NavigateDashboardCommand}"/>
        // CanExecute prüft: IsLoggedIn (Ausnahme: Login-Seite ist immer zugänglich)
        // Quelle ICommand: https://learn.microsoft.com/dotnet/api/system.windows.input.icommand

        public RelayCommand NavigateDashboardCommand { get; }
        public RelayCommand NavigateFoodCommand { get; }
        public RelayCommand NavigateRecipesCommand { get; }
        public RelayCommand NavigateMealPlanCommand { get; }
        public RelayCommand NavigateShoppingCommand { get; }
        public RelayCommand NavigateSettingsCommand { get; }
        public RelayCommand NavigateLoginCommand { get; }

        // ── HILFSPROPERTIES FÜR COMMAND-BEDINGUNGEN ───────────────────────────────
        // Werden in CanExecute-Lambdas genutzt um Buttons zu sperren wenn nicht eingeloggt

        // true = jemand ist eingeloggt (CurrentUser != null)
        public bool IsLoggedIn => UserSession.CurrentUser != null;

        // true = eingeloggt UND Admin-Rolle
        public bool IsAdmin => UserSession.IsAdmin;

        // ── KONSTRUKTOR ───────────────────────────────────────────────────────────
        public MainViewModel()
        {
            // Sub-ViewModels erstellen – Reihenfolge egal, da keine gegenseitigen Abhängigkeiten
            LoginVM    = new LoginViewModel();
            DashboardVM = new DashboardViewModel();
            FoodVM     = new FoodViewModel();
            RecipesVM  = new RecipesViewModel();
            MealPlanVM = new MealPlanViewModel();
            ShoppingVM = new ShoppingListViewModel();
            SettingsVM = new SettingsViewModel();

            // Startseite: immer Login (bis CurrentUserChanged den Wechsel auslöst)
            _currentViewModel = LoginVM;

            // ── NAVIGATIONSBEFEHLE INITIALISIEREN ─────────────────────────────────
            // Jeder Command: () => NavigateTo(XxxVM), () => IsLoggedIn (Bedingung)
            NavigateDashboardCommand = new RelayCommand(
                () => NavigateTo(DashboardVM),
                // Dashboard nur wenn eingeloggt (kein Login = kein Zugriff)
                () => IsLoggedIn);

            NavigateFoodCommand = new RelayCommand(
                () => NavigateTo(FoodVM),
                () => IsLoggedIn);

            NavigateRecipesCommand = new RelayCommand(
                () => NavigateTo(RecipesVM),
                // Rezepte nur für Admins sichtbar (CanEdit-Logik)
                () => IsLoggedIn && IsAdmin);

            NavigateMealPlanCommand = new RelayCommand(
                () => NavigateTo(MealPlanVM),
                () => IsLoggedIn);

            NavigateShoppingCommand = new RelayCommand(
                () => NavigateTo(ShoppingVM),
                () => IsLoggedIn);

            NavigateSettingsCommand = new RelayCommand(
                () => NavigateTo(SettingsVM),
                () => IsLoggedIn);

            // Login-Seite immer zugänglich (auch für ausgeloggte User)
            NavigateLoginCommand = new RelayCommand(
                () => NavigateTo(LoginVM));

            // ── AUF LOGIN/LOGOUT REAGIEREN ─────────────────────────────────────────
            // UserSession feuert dieses Event bei jedem Benutzerwechsel
            // → OnUserChanged() aktualisiert Commands und navigiert
            UserSession.CurrentUserChanged += OnUserChanged;

            // ── SEITENÜBERGREIFENDE KOMMUNIKATION: ZUTAT AUSWÄHLEN ─────────────────
            // RecipesVM signalisiert: "Ich brauche einen Lebensmittelnamen vom User"
            // Quelle C# Events: https://learn.microsoft.com/dotnet/csharp/programming-guide/events/
            RecipesVM.RequestPickFood += () =>
            {
                // FoodVM in Auswahlmodus setzen:
                // onChosen-Callback wird aufgerufen sobald User ein Item wählt
                FoodVM.StartSelectionMode(food =>
                {
                    // Gewählten Namen zurück zu RecipesVM übermitteln
                    RecipesVM.SetPickedFoodName(food.Name);
                    // Auswahlmodus beenden → FoodView verhält sich wieder normal
                    FoodVM.EndSelectionMode();
                    // Zurück zur Rezepte-Seite navigieren
                    NavigateTo(RecipesVM);
                });
                // Zur Food-Seite wechseln damit User etwas auswählen kann
                NavigateTo(FoodVM);
            };

            // ── SEITENÜBERGREIFENDE KOMMUNIKATION: REZEPT AUSWÄHLEN ───────────────
            // MealPlanVM signalisiert: "Ich brauche ein Rezept-Objekt vom User"
            MealPlanVM.RequestPickRecipe += () =>
            {
                // RecipesVM in Auswahlmodus setzen
                RecipesVM.StartSelectionMode(recipe =>
                {
                    // Gewähltes Rezept zurück zu MealPlanVM übermitteln
                    MealPlanVM.SetPickedRecipe(recipe);
                    // Auswahlmodus beenden
                    RecipesVM.EndSelectionMode();
                    // Zurück zum Wochenplan navigieren
                    NavigateTo(MealPlanVM);
                });
                // Zur Rezepte-Seite wechseln
                NavigateTo(RecipesVM);
            };
        }

        // --------------------------------------------------------
        // NavigateTo
        //
        // FUNKTION:
        //   Setzt CurrentViewModel → WPF zeigt passende View an.
        //   ContentControl in MainWindow.xaml reagiert sofort per Binding.
        //
        // PARAMETER:
        //   viewModel → das ViewModel der Zielseite
        //
        // RETURN: void – Seitenänderung geschieht über Property-Binding
        // --------------------------------------------------------
        private void NavigateTo(BaseViewModel viewModel)
        {
            // SetProperty → OnPropertyChanged("CurrentViewModel")
            // → ContentControl liest neuen Wert → DataTemplate wählt passende View
            CurrentViewModel = viewModel;
        }

        // --------------------------------------------------------
        // OnUserChanged
        //
        // FUNKTION:
        //   Wird aufgerufen wenn sich der eingeloggte User ändert
        //   (Login, Logout, Profilupdate).
        //   Aktualisiert alle Command-Zustände und navigiert zur richtigen Seite.
        //
        // NACH LOGIN  → CurrentViewModel = DashboardVM (Startseite)
        // NACH LOGOUT → CurrentViewModel = LoginVM
        //
        // RaiseCanExecuteChanged(): teilt WPF mit dass es CanExecute
        // aller Navigation-Commands neu prüfen soll.
        // Quelle: https://learn.microsoft.com/dotnet/api/system.windows.input.icommand.canexecutechanged
        // --------------------------------------------------------
        private void OnUserChanged()
        {
            // Alle Navigation-Commands neu prüfen:
            // IsLoggedIn hat sich geändert → Buttons aktiv/inaktiv neu zeichnen
            NavigateDashboardCommand.RaiseCanExecuteChanged();
            NavigateFoodCommand.RaiseCanExecuteChanged();
            NavigateRecipesCommand.RaiseCanExecuteChanged();
            NavigateMealPlanCommand.RaiseCanExecuteChanged();
            NavigateShoppingCommand.RaiseCanExecuteChanged();
            NavigateSettingsCommand.RaiseCanExecuteChanged();

            // Auch IsLoggedIn und IsAdmin Properties aktualisieren
            // (für etwaige direkte Bindings im XAML)
            OnPropertyChanged(nameof(IsLoggedIn));
            OnPropertyChanged(nameof(IsAdmin));

            if (UserSession.CurrentUser == null)
                // Ausgeloggt → Login-Seite zeigen
                NavigateTo(LoginVM);
            else
                // Eingeloggt → Dashboard als Einstiegspunkt
                NavigateTo(DashboardVM);
        }
    }
}