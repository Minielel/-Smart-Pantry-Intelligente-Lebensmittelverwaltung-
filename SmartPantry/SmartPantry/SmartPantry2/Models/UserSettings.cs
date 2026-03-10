// ============================================================
// Datei:   UserSettings.cs
// Schicht: Model / Datenmodell
// Datenbanktabelle: user_settings
//
// ZWECK:
//   Speichert die benutzerspezifischen App-Einstellungen.
//   Jeder User hat genau einen Settings-Datensatz (1:1 mit users).
//
// ROTER FADEN:
//   users ──→ user_settings (1:1, über user_id)
//
//   LIFECYCLE:
//   1. AuthService.Register() → legt Standard-Datensatz an
//      (Theme="green", Language="de")
//   2. AuthService.Login() → lädt Settings per Include(u => u.Settings)
//   3. SettingsViewModel.Load() → wendet Theme + Sprache über
//      ResourceSwapService sofort auf die UI an
//   4. SettingsViewModel.SaveSettings() → schreibt Änderungen in DB
//      → beim nächsten Login wieder geladen
//
//   Theme-Werte:   "green" | "blue" | "orange"
//   Language-Werte: "de" | "en"
//   Diese Strings werden in SettingsViewModel.ApplyThemeResources()
//   auf XAML-Dateipfade gemappt (z.B. "blue" → "Resources/Theme.Blue.xaml")
//
// USER USECASE:
//   User öffnet Settings
//   → klickt "Blue" → Farbe ändert sich sofort (nicht gespeichert)
//   → klickt "Speichern" → in DB persistiert
//   → nächstes Login: Theme wird automatisch wiederhergestellt
//
// QUELLEN:
//   EF Core – One-to-One Relationships:
//   https://learn.microsoft.com/ef/core/modeling/relationships/one-to-one
// ============================================================

namespace Smartpantry.Models
{
    public class UserSettings
    {
        // Primärschlüssel → DB-Spalte "id"
        public int Id { get; set; }

        // Fremdschlüssel zu users.id → DB-Spalte "user_id"
        // ON DELETE CASCADE: wenn User gelöscht → Settings auch weg
        public int UserId { get; set; }

        // Aktives Theme → DB-Spalte "theme"
        // Mögliche Werte: "green" | "blue" | "orange"
        // Wird in SettingsViewModel.ApplyThemeResources() verarbeitet:
        //   "green"  → Resources/Theme.Green.xaml
        //   "blue"   → Resources/Theme.Blue.xaml
        //   "orange" → Resources/Theme.Orange.xaml
        public string Theme { get; set; }

        // Aktive Sprache → DB-Spalte "language"
        // Mögliche Werte: "de" | "en"
        // Wird in SettingsViewModel.ApplyLanguageResources() verarbeitet:
        //   "de" → Resources/Strings.de.xaml
        //   "en" → Resources/Strings.en.xaml
        public string Language { get; set; }

        // Navigationsproperty: der zugehörige User (Rückreferenz)
        public User User { get; set; }
    }
}