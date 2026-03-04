using Smartpantry.Helpers;
using Smartpantry.Models;
using SmartPantry2.Services;
using System;

namespace Smartpantry.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        private readonly AuthService _authService = new AuthService();

        private string _username = "";
        public string Username
        {
            get => _username;
            set
            {
                if (SetProperty(ref _username, value))
                {
                    LoginCommand.RaiseCanExecuteChanged();
                    RegisterCommand.RaiseCanExecuteChanged();
                }
            }
        }

        private string _password = "";
        public string Password
        {
            get => _password;
            set
            {
                if (SetProperty(ref _password, value))
                {
                    LoginCommand.RaiseCanExecuteChanged();
                    RegisterCommand.RaiseCanExecuteChanged();
                }
            }
        }

        private string _email = "";
        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        private string _statusMessage = "";
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    LoginCommand.RaiseCanExecuteChanged();
                    RegisterCommand.RaiseCanExecuteChanged();
                    LogoutCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public RelayCommand LoginCommand { get; }
        public RelayCommand RegisterCommand { get; }
        public RelayCommand LogoutCommand { get; }

        public LoginViewModel()
        {
            LoginCommand = new RelayCommand(Login, CanLoginOrRegister);
            RegisterCommand = new RelayCommand(Register, CanLoginOrRegister);
            LogoutCommand = new RelayCommand(Logout, () => !IsBusy && UserSession.CurrentUser != null);

            UserSession.CurrentUserChanged += () =>
            {
                OnPropertyChanged(nameof(IsLoggedIn));
                LogoutCommand.RaiseCanExecuteChanged();
            };
        }

        public bool IsLoggedIn => UserSession.CurrentUser != null;

        private bool CanLoginOrRegister()
        {
            return !IsBusy
                && !string.IsNullOrWhiteSpace(Username)
                && !string.IsNullOrWhiteSpace(Password);
        }

        private void Login()
        {
            try
            {
                IsBusy = true;
                StatusMessage = "";

                var user = _authService.Login(Username.Trim(), Password);
                if (user == null)
                {
                    StatusMessage = "Login fehlgeschlagen. Username/Passwort prüfen.";
                    return;
                }

                UserSession.CurrentUser = user;
                StatusMessage = $"Eingeloggt als {user.Username}.";
            }
            catch (Exception ex)
            {
                StatusMessage = "Fehler beim Login: " + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void Register()
        {
            try
            {
                IsBusy = true;
                StatusMessage = "";

                var user = new User
                {
                    Username = Username.Trim(),
                    Email = string.IsNullOrWhiteSpace(Email) ? $"{Username.Trim()}@example.com" : Email.Trim(),
                    Role = "User"
                };

                bool ok = _authService.Register(user, Password);
                StatusMessage = ok
                    ? "Registrierung erfolgreich. Du kannst dich jetzt einloggen."
                    : "Registrierung fehlgeschlagen: Username existiert bereits.";
            }
            catch (Exception ex)
            {
                StatusMessage = "Fehler bei Registrierung: " + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void Logout()
        {
            UserSession.Logout();
            StatusMessage = "Abgemeldet.";
        }
    }
}
