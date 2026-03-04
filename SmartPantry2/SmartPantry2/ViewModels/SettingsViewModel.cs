using Smartpantry.Helpers;
using SmartPantry2.Services;
using System;

namespace Smartpantry.ViewModels
{
    public class SettingsViewModel : BaseViewModel
    {
        private readonly SettingsService _settingsService = new SettingsService();

        private string _theme = "Light";
        public string Theme { get => _theme; set => SetProperty(ref _theme, value); }

        private string _language = "de";
        public string Language { get => _language; set => SetProperty(ref _language, value); }

        private string _statusMessage = "";
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

        public RelayCommand LoadCommand { get; }
        public RelayCommand SaveCommand { get; }

        public SettingsViewModel()
        {
            LoadCommand = new RelayCommand(Load);
            SaveCommand = new RelayCommand(Save);

            UserSession.CurrentUserChanged += Load;
            Load();
        }

        public void Load()
        {
            try
            {
                StatusMessage = "";
                var userId = UserSession.CurrentUserId;
                if (userId == null) return;

                var s = _settingsService.Get(userId.Value);
                if (s == null)
                {
                    Theme = "Light";
                    Language = "de";
                    return;
                }

                Theme = s.Theme ?? "Light";
                Language = s.Language ?? "de";
            }
            catch (Exception ex)
            {
                StatusMessage = "Fehler beim Laden der Settings: " + ex.Message;
            }
        }

        public void Save()
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
                    StatusMessage = "Keine Settings gefunden (UserSettings fehlt in DB).";
                    return;
                }

                existing.Theme = Theme ?? "Light";
                existing.Language = Language ?? "de";
                _settingsService.Update(existing);

                StatusMessage = "Settings gespeichert.";
            }
            catch (Exception ex)
            {
                StatusMessage = "Fehler beim Speichern: " + ex.Message;
            }
        }
    }
}
