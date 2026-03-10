// ============================================================
// Datei:   SettingsViewModel.cs
// Schicht: ViewModel / Einstellungen
//
// ZWECK:
//   Verwaltet Theme, Sprache, Profil und Passwort-Änderungen.
//   Wendet Theme und Sprache sofort per ResourceSwapService an
//   (Live-Vorschau ohne App-Neustart).
//
// ROTER FADEN:
//   SettingsView.xaml ←→ SettingsViewModel
//   ←→ SettingsService  → DB: user_settings  (Theme + Sprache)
//   ←→ AuthService      → DB: users           (Profil + Passwort)
//   ←→ ResourceSwapService → WPF ResourceDictionaries tauschen
//
//   THEME-WECHSEL FLOW:
//   User klickt "Blue" → SetTheme("blue")
//     → SelectedTheme = "blue"
//     → ApplyThemeResources("blue")
//       → ResourceSwapService.SwapMergedDictionary("Theme.", Theme.Blue.xaml)
//         → WPF tauscht Farb-Dictionary sofort aus
//         → alle DynamicResource-Brushes (AccentBrush, SidebarBrush) aktualisieren sich
//
//   SPRACH-WECHSEL FLOW:
//   User klickt "English" → SetLanguage("en")
//     → SelectedLanguage = "en"
//     → ApplyLanguageResources("en")
//       → ResourceSwapService.SwapMergedDictionary("Strings.", Strings.en.xaml)
//         → alle {DynamicResource Nav_Dashboard} zeigen englische Texte
//
//   "SPEICHERN":
//   → SettingsService.Update() → UPDATE user_settings SET theme=?, language=?
//   → AuthService.UpdateProfile() → UPDATE users SET username=?, email=?, role=?
//   → UserSession.CurrentUser aktualisieren (damit CanEdit sofort stimmt)
//
// USER USECASE:
//   User öffnet "Settings"
//   → aktuelle Einstellungen werden geladen (Theme, Sprache, Profil)
//   User klickt "Blue" → Farben ändern sich sofort (noch nicht gespeichert!)
//   User klickt "English" → Menüpunkte auf Englisch (noch nicht gespeichert!)
//   User klickt "Speichern" → alles in DB persistiert
//   → nächstes Login: alles wird automatisch wieder angewendet
//
// QUELLEN:
//   WPF ResourceDictionary / MergedDictionaries (für Theme-Wechsel):
//   https://learn.microsoft.com/dotnet/desktop/wpf/systems/xaml-resources-merged-dictionaries
//
//   DynamicResource vs StaticResource:
//   https://learn.microsoft.com/dotnet/desktop/wpf/systems/xaml-resources-dynamic-static-comparison
//
//   Uri (Klasse für Ressourcen-Pfade):
//   https://learn.microsoft.com/dotnet/api/system.uri
//
//   BCrypt.Net-Next (Passwort-Hashing in AuthService):
//   https://github.com/BcryptNet/bcrypt.net
// ============================================================

using Smartpantry.Helpers;
using Smartpantry.Models;
using SmartPantry2.Services;
using System;

namespace Smartpantry.ViewModels
{
    public class SettingsViewModel : BaseViewModel
    {
        // Services für die verschiedenen Datenbankoperationen
        private readonly SettingsService _settingsService = new SettingsService();
        private readonly AuthService     _authService     = new AuthService();

        // ── EINSTELLUNGS-PROPERTIES ───────────────────────────────────────────────
        // Alle gebunden an Buttons/Textfelder in SettingsView.xaml

        // Aktuell ausgewähltes Theme: "green" | "blue" | "orange"
        // Steuert welcher Theme-Button als "aktiv" dargestellt wird
        private string _selectedTheme = "green";
        public string SelectedTheme
        {
            get => _selectedTheme;
            set => SetProperty(ref _selectedTheme, value);
        }

        // Aktuell ausgewählte Sprache: "de" | "en"
        private string _selectedLanguage = "de";
        public string SelectedLanguage
        {
            get => _selectedLanguage;
            set => SetProperty(ref _selectedLanguage, value);
        }

        // ── PROFIL-FELDER ─────────────────────────────────────────────────────────
        private string _username = "";
        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        private string _email = "";
        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        // Rolle: "admin" oder "standard"
        // Admins können hier ihren eigenen Account zu Standard downgraden (oder upgraden)
        private string _role = "standard";
        public string Role
        {
            get => _role;
            set => SetProperty(ref _role, value);
        }

        // ── PASSWORT-ÄNDERUNG ─────────────────────────────────────────────────────
        private string _currentPassword = "";
        // Aktuelles Passwort (zur Verifikation vor der Änderung)
        public string CurrentPassword
        {
            get => _currentPassword;
            set => SetProperty(ref _currentPassword, value);
        }

        private string _newPassword = "";
        // Neues Passwort (wird zu BCrypt-Hash in AuthService.ChangePassword)
        public string NewPassword
        {
            get => _newPassword;
            set => SetProperty(ref _newPassword, value);
        }

        // ── STATUS-MELDUNG ────────────────────────────────────────────────────────
        private string _statusMessage = "";
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        // ── BERECHTIGUNGEN ────────────────────────────────────────────────────────
        // Nur Admin darf Rolle ändern (Standard-User sieht Rolle-Feld schreibgeschützt)
        public bool CanEdit => UserSession.IsAdmin;

        // ── COMMANDS ──────────────────────────────────────────────────────────────
        // "Speichern": speichert Theme + Sprache + Profil in DB
        public RelayCommand SaveSettingsCommand { get; }

        // "Passwort ändern": prüft altes Passwort und setzt neues
        public RelayCommand ChangePasswordCommand { get; }

        // Theme-Buttons: sofortiger Live-Wechsel (noch nicht gespeichert)
        // Jeder Button übergibt seinen Theme-Namen als Parameter
        public RelayCommand<string> SetThemeCommand { get; }

        // Sprach-Buttons: sofortiger Live-Wechsel (noch nicht gespeichert)
        public RelayCommand<string> SetLanguageCommand { get; }

        // Logout-Button in SettingsView
        public RelayCommand LogoutCommand { get; }

        public SettingsViewModel()
        {
            SaveSettingsCommand    = new RelayCommand(SaveSettings);
            ChangePasswordCommand  = new RelayCommand(ChangePassword);
            // SetThemeCommand bekommt "green", "blue" oder "orange" als Parameter
            SetThemeCommand        = new RelayCommand<string>(SetTheme);
            // SetLanguageCommand bekommt "de" oder "en" als Parameter
            SetLanguageCommand     = new RelayCommand<string>(SetLanguage);
            // Logout: UserSession.Logout() → CurrentUser=null → MainViewModel navigiert zu Login
            LogoutCommand          = new RelayCommand(UserSession.Logout);

            // Bei Login: Einstellungen laden und anwenden
            UserSession.CurrentUserChanged += Load;

            Load();
        }

        // --------------------------------------------------------
        // Load
        //
        // FUNKTION:
        //   Lädt aktuelle Einstellungen aus dem eingeloggten User
        //   und wendet Theme + Sprache sofort an.
        //
        // DATENQUELLE:
        //   UserSession.CurrentUser.Settings (bereits per Include() beim Login geladen!)
        //   Falls Settings fehlen → SettingsService.Get() erneut aus DB laden
        //
        // AUFGERUFEN VON:
        //   Konstruktor, UserSession.CurrentUserChanged
        // --------------------------------------------------------
        private void Load()
        {
            // Wenn niemand eingeloggt → Felder leeren
            if (UserSession.CurrentUser == null)
            {
                Username = ""; Email = ""; Role = "standard";
                return;
            }

            var user = UserSession.CurrentUser;

            // Profil-Felder aus User-Objekt befüllen
            Username = user.Username ?? "";
            Email    = user.Email    ?? "";
            Role     = user.Role     ?? "standard";

            // Einstellungen aus dem mitgeladenen Settings-Objekt lesen
            // (von AuthService.Login() per Include(u => u.Settings) mitgeladen)
            var settings = user.Settings
                // Fallback: direkt aus DB laden wenn Settings nicht im Objekt
                ?? _settingsService.Get(user.Id);

            if (settings != null)
            {
                SelectedTheme    = settings.Theme    ?? "green";
                SelectedLanguage = settings.Language ?? "de";

                // Theme und Sprache sofort auf UI anwenden
                ApplyThemeResources(SelectedTheme);
                ApplyLanguageResources(SelectedLanguage);
            }

            OnPropertyChanged(nameof(CanEdit));
        }

        // --------------------------------------------------------
        // SetTheme
        //
        // FUNKTION:
        //   Setzt das aktive Theme und wendet es sofort an (Live-Vorschau).
        //   ACHTUNG: noch NICHT in DB gespeichert! → erst nach SaveSettings()
        //
        // PARAMETER: theme → "green" | "blue" | "orange"
        //
        // AUFGERUFEN VON: SetThemeCommand (Theme-Button-Klick)
        //   In SettingsView.xaml:
        //     <Button Command="{Binding SetThemeCommand}" CommandParameter="blue"/>
        // --------------------------------------------------------
        private void SetTheme(string? theme)
        {
            // null-Check: defensiv gegen ungültige Parameter
            if (string.IsNullOrWhiteSpace(theme)) return;

            // SelectedTheme setzen → WPF markiert den aktiven Theme-Button
            SelectedTheme = theme;
            // Sofort anwenden → alle DynamicResource-Brushes wechseln
            ApplyThemeResources(theme);
        }

        // --------------------------------------------------------
        // ApplyThemeResources
        //
        // FUNKTION:
        //   Tauscht das WPF ResourceDictionary für das Theme aus.
        //   → AccentBrush + SidebarBrush werden sofort aktualisiert
        //
        // MAPPING:
        //   "green"  → Resources/Theme.Green.xaml  → AccentColor #7BCB4E
        //   "blue"   → Resources/Theme.Blue.xaml   → AccentColor #3B82F6
        //   "orange" → Resources/Theme.Orange.xaml → AccentColor #F59E0B
        //
        // ResourceSwapService.SwapMergedDictionary() tauscht das Dictionary
        // in Application.Current.Resources.MergedDictionaries aus.
        // DynamicResource in XAML: Steuerelemente reagieren sofort!
        // (StaticResource würde NICHT reagieren)
        //
        // Quelle DynamicResource:
        //   https://learn.microsoft.com/dotnet/desktop/wpf/systems/xaml-resources-dynamic-static-comparison
        // --------------------------------------------------------
        private void ApplyThemeResources(string theme)
        {
            // Dateiname je nach gewähltem Theme bestimmen
            var fileName = theme.ToLower() switch
            {
                // C# 8 switch expression:
                // Quelle: https://learn.microsoft.com/dotnet/csharp/language-reference/operators/switch-expression
                "blue"   => "Theme.Blue.xaml",
                "orange" => "Theme.Orange.xaml",
                // "green" und alle anderen: grünes Theme als Fallback
                _        => "Theme.Green.xaml"
            };

            // Uri: relativer Pfad zur XAML-Ressource
            // UriKind.Relative: Pfad relativ zum Projekt-Ausgabeverzeichnis
            // Quelle: https://learn.microsoft.com/dotnet/api/system.uri
            var uri = new Uri($"Resources/{fileName}", UriKind.Relative);

            // ResourceSwapService tauscht das Dictionary in MergedDictionaries aus
            // "Theme." = Suchpräfix: findet "Resources/Theme.Green.xaml" usw.
            ResourceSwapService.SwapMergedDictionary("Theme.", uri);
        }

        // --------------------------------------------------------
        // SetLanguage
        //
        // FUNKTION:
        //   Setzt die aktive Sprache und wendet sie sofort an (Live-Vorschau).
        //   ACHTUNG: noch NICHT in DB gespeichert! → erst nach SaveSettings()
        //
        // PARAMETER: language → "de" | "en"
        //
        // AUFGERUFEN VON: SetLanguageCommand
        //   In SettingsView.xaml:
        //     <Button Command="{Binding SetLanguageCommand}" CommandParameter="en"/>
        // --------------------------------------------------------
        private void SetLanguage(string? language)
        {
            if (string.IsNullOrWhiteSpace(language)) return;

            SelectedLanguage = language;
            ApplyLanguageResources(language);
        }

        // --------------------------------------------------------
        // ApplyLanguageResources
        //
        // FUNKTION:
        //   Tauscht das WPF ResourceDictionary für die Sprache aus.
        //   → alle {DynamicResource Nav_Dashboard} usw. wechseln sofort
        //
        // MAPPING:
        //   "en" → Resources/Strings.en.xaml (englische Texte)
        //   "de" → Resources/Strings.de.xaml (deutsche Texte, Standard)
        // --------------------------------------------------------
        private void ApplyLanguageResources(string language)
        {
            // Dateiname je nach Sprache
            var fileName = language.ToLower() == "en"
                ? "Strings.en.xaml"
                : "Strings.de.xaml";

            var uri = new Uri($"Resources/{fileName}", UriKind.Relative);

            // "Strings." = Suchpräfix: findet "Resources/Strings.de.xaml" usw.
            ResourceSwapService.SwapMergedDictionary("Strings.", uri);
        }

        // --------------------------------------------------------
        // SaveSettings
        //
        // FUNKTION:
        //   Speichert alle Einstellungen dauerhaft in der DB:
        //   1. user_settings: Theme + Sprache
        //   2. users: Username + Email + Rolle
        //   Aktualisiert danach UserSession.CurrentUser.
        //
        // DB-Zugriffe:
        //   UPDATE user_settings SET theme=?, language=? WHERE user_id=?
        //   UPDATE users SET username=?, email=?, role=? WHERE id=?
        //
        // AUFGERUFEN VON: SaveSettingsCommand (Button "Speichern" in SettingsView)
        // --------------------------------------------------------
        private void SaveSettings()
        {
            if (UserSession.CurrentUser == null) return;

            try
            {
                var userId = UserSession.CurrentUser.Id;

                // ── SETTINGS SPEICHERN ──────────────────────────────────────────
                // Vorhandene Settings laden oder neue erstellen
                var settings = _settingsService.Get(userId);

                if (settings == null)
                {
                    // Noch keine Settings vorhanden → neu anlegen
                    _settingsService.Add(new UserSettings
                    {
                        UserId   = userId,
                        Theme    = SelectedTheme,
                        Language = SelectedLanguage
                    });
                }
                else
                {
                    // Vorhandene Settings aktualisieren
                    settings.Theme    = SelectedTheme;
                    settings.Language = SelectedLanguage;
                    _settingsService.Update(settings);
                }

                // ── PROFIL SPEICHERN ────────────────────────────────────────────
                // AuthService prüft Username-Duplikate und schreibt UPDATE
                bool profileSaved = _authService.UpdateProfile(
                    userId,
                    Username.Trim(),
                    Email.Trim(),
                    Role);

                if (!profileSaved)
                {
                    StatusMessage = "Fehler: Benutzername bereits vergeben.";
                    return;
                }

                // ── SESSION AKTUALISIEREN ───────────────────────────────────────
                // UserSession.CurrentUser muss aktualisiert werden damit
                // CanEdit und IsAdmin sofort die neuen Werte liefern
                UserSession.CurrentUser.Username = Username.Trim();
                UserSession.CurrentUser.Email    = Email.Trim();
                UserSession.CurrentUser.Role     = Role;

                // UserSession neu setzen → CurrentUserChanged feuert
                // → alle ViewModels aktualisieren CanEdit und laden neu
                UserSession.CurrentUser = UserSession.CurrentUser;

                StatusMessage = "Einstellungen gespeichert!";
            }
            catch (Exception ex)
            {
                StatusMessage = "Fehler beim Speichern: " + ex.Message;
            }
        }

        // --------------------------------------------------------
        // ChangePassword
        //
        // FUNKTION:
        //   Prüft das aktuelle Passwort (BCrypt.Verify in AuthService)
        //   und ersetzt es durch einen neuen Hash.
        //
        // SICHERHEIT:
        //   Das aktuelle Passwort muss korrekt sein (keine Änderung ohne Auth).
        //   Das neue Passwort wird von AuthService.ChangePassword() gehasht.
        //
        // DB: UPDATE users SET password_hash=? WHERE id=?
        // --------------------------------------------------------
        private void ChangePassword()
        {
            if (UserSession.CurrentUser == null) return;

            // Eingabe-Validierung
            if (string.IsNullOrWhiteSpace(CurrentPassword) ||
                string.IsNullOrWhiteSpace(NewPassword))
            {
                StatusMessage = "Bitte aktuelles und neues Passwort eingeben.";
                return;
            }

            try
            {
                // AuthService.ChangePassword():
                // BCrypt.Verify(CurrentPassword, PasswordHash) → prüft altes Passwort
                // BCrypt.HashPassword(NewPassword) → neuen Hash speichern
                bool changed = _authService.ChangePassword(
                    UserSession.CurrentUser.Id,
                    CurrentPassword,
                    NewPassword);

                if (changed)
                {
                    StatusMessage    = "Passwort erfolgreich geändert.";
                    // Felder leeren nach Erfolg
                    CurrentPassword = "";
                    NewPassword     = "";
                }
                else
                {
                    // AuthService.ChangePassword() gibt false zurück wenn
                    // aktuelles Passwort falsch ist
                    StatusMessage = "Aktuelles Passwort ist falsch.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "Fehler beim Passwort ändern: " + ex.Message;
            }
        }
    }
}