// ------------------------------------------------------------
// Datei: SettingsViewModel.cs
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

namespace Smartpantry.ViewModels
{
    public class SettingsViewModel : BaseViewModel
    {
        private readonly SettingsService _settingsService = new SettingsService();
        private readonly AuthService _authService = new AuthService();

        public bool CanEditRole => true;


        private string _selectedTheme = "green";
        public string SelectedTheme
        {
            get => _selectedTheme;
            set => SetProperty(ref _selectedTheme, value);
        }

        private string _selectedLanguage = "de";
        public string SelectedLanguage
        {
            get => _selectedLanguage;
            set => SetProperty(ref _selectedLanguage, value);
        }


        private string _username = "";
        public string Username { get => _username; set => SetProperty(ref _username, value); }

        private string _email = "";
        public string Email { get => _email; set => SetProperty(ref _email, value); }

        private string _role = "standard";
        public string Role { get => _role; set => SetProperty(ref _role, value); }


        private string _currentPassword = "";
        public string CurrentPassword { get => _currentPassword; set => SetProperty(ref _currentPassword, value); }

        private string _newPassword = "";
        public string NewPassword { get => _newPassword; set => SetProperty(ref _newPassword, value); }

        private string _statusMessage = "";
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

        public RelayCommand LoadCommand { get; }
        public RelayCommand SaveSettingsCommand { get; }
        public RelayCommand SaveProfileCommand { get; }
        public RelayCommand ChangePasswordCommand { get; }

        public RelayCommand<string> SetThemeCommand { get; }
        public RelayCommand<string> SetLanguageCommand { get; }

        public SettingsViewModel()
        {
            LoadCommand = new RelayCommand(Load);
            SaveSettingsCommand = new RelayCommand(SaveSettings);
            SaveProfileCommand = new RelayCommand(SaveProfile);
            ChangePasswordCommand = new RelayCommand(ChangePassword);

            SetThemeCommand = new RelayCommand<string>(SetTheme);
            SetLanguageCommand = new RelayCommand<string>(SetLanguage);

            UserSession.CurrentUserChanged += () => { Load(); OnPropertyChanged(nameof(CanEditRole)); };
            Load();
        }

        public void Load()
        {
            try
            {
                StatusMessage = "";
                var user = UserSession.CurrentUser;
                if (user == null) return;

                Username = user.Username ?? "";
                Email = user.Email ?? "";
                Role = string.IsNullOrWhiteSpace(user.Role) ? "standard" : user.Role;

                var s = _settingsService.Get(user.Id);
                if (s != null)
                {
                    SelectedTheme = string.IsNullOrWhiteSpace(s.Theme) ? "green" : s.Theme;
                    SelectedLanguage = string.IsNullOrWhiteSpace(s.Language) ? "de" : s.Language;
                }


                ApplyThemeResources(SelectedTheme);
                ApplyLanguageResources(SelectedLanguage);
            }
            catch (Exception ex)
            {
                StatusMessage = "Fehler beim Laden: " + ex.Message;
            }
        }

        private void SetTheme(string? theme)
        {
            if (string.IsNullOrWhiteSpace(theme)) return;
            SelectedTheme = theme;
            ApplyThemeResources(theme);
        }

        private void SetLanguage(string? lang)
        {
            if (string.IsNullOrWhiteSpace(lang)) return;
            SelectedLanguage = lang;
            ApplyLanguageResources(lang);
        }

        private void ApplyThemeResources(string theme)
        {
            var uri = theme switch
            {
                "blue" => new Uri("Resources/Theme.Blue.xaml", UriKind.Relative),
                "orange" => new Uri("Resources/Theme.Orange.xaml", UriKind.Relative),
                _ => new Uri("Resources/Theme.Green.xaml", UriKind.Relative),
            };
            ResourceSwapService.SwapMergedDictionary("Theme.", uri);
        }

        private void ApplyLanguageResources(string lang)
        {
            var uri = lang switch
            {
                "en" => new Uri("Resources/Strings.en.xaml", UriKind.Relative),
                _ => new Uri("Resources/Strings.de.xaml", UriKind.Relative),
            };
            ResourceSwapService.SwapMergedDictionary("Strings.", uri);
        }

        private void SaveSettings()
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

                var existing = _settingsService.Get(userId.Value);
                if (existing == null)
                {

                    existing = new UserSettings { UserId = userId.Value };
                    _settingsService.Add(existing);
                }

                existing.Theme = SelectedTheme;
                existing.Language = SelectedLanguage;
                _settingsService.Update(existing);

                StatusMessage = "Settings gespeichert.";
            }
            catch (Exception ex)
            {
                StatusMessage = "Fehler beim Speichern: " + ex.Message;
            }
        }

        private void SaveProfile()
        {
            try
            {
                StatusMessage = "";
                var user = UserSession.CurrentUser;
                if (user == null)
                {
                    StatusMessage = "Bitte zuerst einloggen.";
                    return;
                }

                var username = (Username ?? "").Trim();
                var email = (Email ?? "").Trim();
                var role = string.IsNullOrWhiteSpace(Role) ? "standard" : Role;

                if (string.IsNullOrWhiteSpace(username))
                {
                    StatusMessage = "Username darf nicht leer sein.";
                    return;
                }

                if (role != "admin" && role != "standard") role = "standard";

                bool ok = _authService.UpdateProfile(user.Id, username, email, role);
                if (!ok)
                {
                    StatusMessage = "Profil konnte nicht gespeichert werden (Username evtl. vergeben).";
                    return;
                }


                user.Username = username;
                user.Email = email;
                user.Role = role;
                UserSession.CurrentUser = user;

                StatusMessage = "Profil gespeichert.";
            }
            catch (Exception ex)
            {
                StatusMessage = "Fehler beim Profil-Speichern: " + ex.Message;
            }
        }

        private void ChangePassword()
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

                if (string.IsNullOrWhiteSpace(CurrentPassword) || string.IsNullOrWhiteSpace(NewPassword))
                {
                    StatusMessage = "Bitte aktuelles und neues Passwort eingeben.";
                    return;
                }

                bool ok = _authService.ChangePassword(userId.Value, CurrentPassword, NewPassword);
                if (!ok)
                {
                    StatusMessage = "Passwort konnte nicht geändert werden (aktuelles Passwort falsch?).";
                    return;
                }

                CurrentPassword = "";
                NewPassword = "";
                StatusMessage = "Passwort geändert.";
            }
            catch (Exception ex)
            {
                StatusMessage = "Fehler beim Passwort-Ändern: " + ex.Message;
            }
        }
    }
}
