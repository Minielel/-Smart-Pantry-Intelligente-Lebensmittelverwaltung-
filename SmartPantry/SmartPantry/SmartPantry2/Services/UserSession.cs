// ------------------------------------------------------------
// Datei: UserSession.cs
//
// Beschreibung:
// Diese Datei gehört zur Service-Schicht. Hier werden Aufgaben wie Datenbankzugriffe, Hilfslogik oder das Nachladen von Daten erledigt.
//
// Hinweis fuer die Vorstellung:
// Wenn man diese Datei in der Schule erklaeren moechte, kann man sagen,
// dass sie einen bestimmten Baustein der App uebernimmt und dadurch hilft,
// die Anwendung klar zu strukturieren.
// ------------------------------------------------------------
using Smartpantry.Models;
using System;

namespace SmartPantry2.Services
{
    public static class UserSession
    {
        private static User? _currentUser;

        public static User? CurrentUser
        {
            get => _currentUser;
            set
            {
                if (_currentUser?.Id == value?.Id) return;
                _currentUser = value;
                CurrentUserChanged?.Invoke();
            }
        }

        public static int? CurrentUserId => CurrentUser?.Id;

        public static string CurrentUserRole => CurrentUser?.Role ?? "standard";
        public static bool IsAdmin => string.Equals(CurrentUserRole, "admin", StringComparison.OrdinalIgnoreCase);

        public static event Action? CurrentUserChanged;

        public static void Logout() => CurrentUser = null;
    }
}
