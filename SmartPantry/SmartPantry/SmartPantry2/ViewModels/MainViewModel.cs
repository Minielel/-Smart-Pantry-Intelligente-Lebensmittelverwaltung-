// ------------------------------------------------------------
// Datei: MainViewModel.cs
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
using SmartPantry2.Services;
using System.Windows.Input;

namespace Smartpantry.ViewModels
{



    public class MainViewModel : BaseViewModel
    {
        // Dieses ViewModel ist so etwas wie die Zentrale der App.
        // Hier wird entschieden, welche Seite gerade angezeigt wird und wie zwischen den Bereichen gewechselt wird.

        public LoginViewModel LoginVM { get; }
        public DashboardViewModel DashboardVM { get; }
        public FoodViewModel FoodVM { get; }
        public RecipesViewModel RecipesVM { get; }
        public MealPlanViewModel MealPlanVM { get; }
        public ShoppingListViewModel ShoppingVM { get; }
        public SettingsViewModel SettingsVM { get; }



        private BaseViewModel _currentViewModel;
        public BaseViewModel CurrentViewModel
        {
            get => _currentViewModel;
            private set => SetProperty(ref _currentViewModel, value);
        }


        public bool IsLoggedIn => UserSession.CurrentUser != null;
        public bool CanAccessRecipes => UserSession.IsAdmin;


        public string CurrentUserLabel
        {
            get
            {
                var u = UserSession.CurrentUser;
                return u == null ? "" : $"Eingeloggt als\n{u.Username}";
            }
        }

        public ICommand NavigateDashboardCommand { get; }
        public ICommand NavigateFoodCommand { get; }
        public ICommand NavigateRecipesCommand { get; }
        public ICommand NavigateMealPlanCommand { get; }
        public ICommand NavigateShoppingCommand { get; }
        public ICommand NavigateSettingsCommand { get; }
        public ICommand LogoutCommand { get; }



        public MainViewModel()
        {
            LoginVM = new LoginViewModel();
            DashboardVM = new DashboardViewModel();
            FoodVM = new FoodViewModel();
            RecipesVM = new RecipesViewModel();
            MealPlanVM = new MealPlanViewModel();
            ShoppingVM = new ShoppingListViewModel();
            SettingsVM = new SettingsViewModel();

            _currentViewModel = LoginVM;

            NavigateDashboardCommand = new RelayCommand(() => NavigateTo(DashboardVM), () => IsLoggedIn);
            NavigateFoodCommand = new RelayCommand(() => NavigateTo(FoodVM), () => IsLoggedIn);
            NavigateRecipesCommand = new RelayCommand(() => NavigateTo(RecipesVM), () => IsLoggedIn && CanAccessRecipes);
            NavigateMealPlanCommand = new RelayCommand(() => NavigateTo(MealPlanVM), () => IsLoggedIn);
            NavigateShoppingCommand = new RelayCommand(() => NavigateTo(ShoppingVM), () => IsLoggedIn);
            NavigateSettingsCommand = new RelayCommand(() => NavigateTo(SettingsVM), () => IsLoggedIn);
            LogoutCommand = new RelayCommand(() => UserSession.Logout(), () => IsLoggedIn);

            UserSession.CurrentUserChanged += OnUserChanged;


            RecipesVM.RequestPickFood += () =>
            {

                FoodVM.StartSelectionMode(food =>
                {
                    RecipesVM.SetPickedFoodName(food.Name);
                    FoodVM.EndSelectionMode();
                    NavigateTo(RecipesVM);
                });
                NavigateTo(FoodVM);
            };

            
            MealPlanVM.RequestPickRecipe += () =>
            {

                RecipesVM.StartSelectionMode(recipe =>
                {
                    MealPlanVM.SetPickedRecipe(recipe);
                    RecipesVM.EndSelectionMode();
                    NavigateTo(MealPlanVM);
                });
                NavigateTo(RecipesVM);
            };

            OnUserChanged();
        }

        private void NavigateTo(BaseViewModel vm)
        {
            CurrentViewModel = vm;
        }

        private void OnUserChanged()
        {
            OnPropertyChanged(nameof(IsLoggedIn));
            OnPropertyChanged(nameof(CurrentUserLabel));
            OnPropertyChanged(nameof(CanAccessRecipes));

            ((RelayCommand)NavigateDashboardCommand).RaiseCanExecuteChanged();
            ((RelayCommand)NavigateFoodCommand).RaiseCanExecuteChanged();
            ((RelayCommand)NavigateRecipesCommand).RaiseCanExecuteChanged();
            ((RelayCommand)NavigateMealPlanCommand).RaiseCanExecuteChanged();
            ((RelayCommand)NavigateShoppingCommand).RaiseCanExecuteChanged();
            ((RelayCommand)NavigateSettingsCommand).RaiseCanExecuteChanged();
            ((RelayCommand)LogoutCommand).RaiseCanExecuteChanged();


            if (UserSession.CurrentUser == null)
                CurrentViewModel = LoginVM;
            else if (!CanAccessRecipes && CurrentViewModel == RecipesVM)
                CurrentViewModel = DashboardVM;
            else
                CurrentViewModel = DashboardVM;
        }
    }
}
