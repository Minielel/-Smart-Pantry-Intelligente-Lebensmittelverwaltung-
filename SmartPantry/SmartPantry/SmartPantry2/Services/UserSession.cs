// ============================================================
// Datei:   UserSession.cs
// Schicht: Service / Session-Management
//
// ZWECK:
//   Globaler, statischer RAM-Speicher für den aktuell
//   eingeloggten User. Kein Datenbankzugriff.
//   Alle ViewModels und Services greifen hierauf zu.
//
// ROTER FADEN:
//   Login-Flow:
//     LoginViewModel.Login() → AuthService.Login() → gibt User zurück
//     → UserSession.CurrentUser = user
//     → CurrentUserChanged-Event feuert
//     → MainViewModel.OnUserChanged() → Navigation zu Dashboard
//     → Alle ViewModels laden ihre Daten neu
//
//   Logout-Flow:
//     UserSession.Logout() → CurrentUser = null
//     → CurrentUserChanged-Event feuert
//     → MainViewModel navigiert zurück zur Login-Seite
//     → Alle ViewModels leeren ihre Listen
//
//   ABONNENTEN von CurrentUserChanged:
//     MainViewModel, FoodViewModel, RecipesViewModel,
//     ShoppingListViewModel, MealPlanViewModel, SettingsViewModel, DashboardViewModel
//     → ALLE reagieren auf Login/Logout
//
// QUELLEN:
//   Static Classes and Members in C#:
//   https://learn.microsoft.com/dotnet/csharp/programming-guide/classes-and-structs/static-classes
//
//   C# Events and Delegates:
//   https://learn.microsoft.com/dotnet/csharp/programming-guide/events/
//
//   Action-Delegate (parameterloser Callback):
//   https://learn.microsoft.com/dotnet/api/system.action
//
//   StringComparison.OrdinalIgnoreCase:
//   https://learn.microsoft.com/dotnet/api/system.stringcomparison
// ============================================================

using Smartpantry.Models;
using System;

namespace SmartPantry2.Services
{
    // "static": Klasse hat nur statische Member, keine Instanz möglich
    // → globaler Zugriff über UserSession.CurrentUser von überall
    // Quelle: https://learn.microsoft.com/dotnet/csharp/programming-guide/classes-and-structs/static-classes
    public static class UserSession
    {
        // Privates Backing-Field: hält das User-Objekt im RAM
        // "?" = nullable: null wenn niemand eingeloggt ist
        private static User? _currentUser;

        // --------------------------------------------------------
        // CurrentUser Property
        //
        // GETTER:
        //   Gibt den aktuell eingeloggten User zurück.
        //   null = niemand ist eingeloggt.
        //
        // SETTER:
        //   Setzt den neuen User und feuert CurrentUserChanged.
        //   Prüft ob sich die UserId wirklich geändert hat,
        //   um doppelte Events zu vermeiden.
        //
        // AUFGERUFEN vom Setter von:
        //   LoginViewModel.Login()    → setzt User nach erfolgreichem Login
        //   SettingsViewModel.SaveProfile() → setzt User nach Profilupdate
        //   UserSession.Logout()      → setzt null
        // --------------------------------------------------------
        public static User? CurrentUser
        {
            get => _currentUser;
            set
            {
                // Kein Event wenn gleicher User nochmal gesetzt wird
                // (z.B. wenn Profil gespeichert aber Id gleich bleibt)
                // "?" = Null-conditional: sicher wenn _currentUser oder value null sind
                if (_currentUser?.Id == value?.Id) return;
                _currentUser = value;
                // Event feuern → alle Abonnenten werden benachrichtigt
                CurrentUserChanged?.Invoke();
            }
        }

        // Shortcut für die häufig benötigte UserId
        // null wenn niemand eingeloggt
        // Wird in allen Services für WHERE user_id = ? genutzt:
        //   db.FoodItems.Where(f => f.UserId == userId.Value)
        // "?." = Null-conditional: gibt null zurück wenn CurrentUser null ist
        public static int? CurrentUserId => CurrentUser?.Id;

        // Gibt die Rolle zurück, "standard" als Fallback wenn nicht eingeloggt
        // "??" = Null-Coalescing: wenn Role null → "standard"
        public static string CurrentUserRole => CurrentUser?.Role ?? "standard";

        // --------------------------------------------------------
        // IsAdmin
        //
        // RETURN: true wenn Role == "admin" (Groß-/Kleinschreibung egal)
        //
        // WIRD ÜBERALL GENUTZT für CanEdit:
        //   public bool CanEdit => UserSession.IsAdmin;
        //   → steuert ob Buttons sichtbar sind und ob Formular aktiv ist
        //
        // OrdinalIgnoreCase: "Admin" == "admin" == "ADMIN" → alle gleich
        // Quelle: https://learn.microsoft.com/dotnet/api/system.string.equals
        // --------------------------------------------------------
        public static bool IsAdmin =>
            string.Equals(CurrentUserRole, "admin", StringComparison.OrdinalIgnoreCase);

        // Event: wird bei jedem Benutzerwechsel gefeuert (Login/Logout/Profilupdate)
        // Action = parameterloser Delegate (kein Rückgabewert, keine Parameter)
        // Alle ViewModels abonnieren dies in ihrem Konstruktor:
        //   UserSession.CurrentUserChanged += () => { Load(); ... };
        public static event Action? CurrentUserChanged;

        // --------------------------------------------------------
        // Logout
        //
        // FUNKTION:
        //   Setzt CurrentUser auf null.
        //   → löst CurrentUserChanged aus
        //   → alle ViewModels leeren ihre Listen
        //   → MainViewModel navigiert zu LoginVM
        // --------------------------------------------------------
        public static void Logout() => CurrentUser = null;
    }
}