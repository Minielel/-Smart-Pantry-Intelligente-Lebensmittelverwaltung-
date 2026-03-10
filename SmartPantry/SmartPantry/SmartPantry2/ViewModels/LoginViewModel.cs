// ============================================================
// Datei:   LoginViewModel.cs
// Schicht: ViewModel / Authentifizierung
//
// ZWECK:
//   Verarbeitet Login und Registrierung.
//   Delegiert die eigentliche Logik an AuthService.
//   Setzt nach erfolgreichem Login UserSession.CurrentUser.
//
// ROTER FADEN:
//   LoginView.xaml ←→ LoginViewModel ←→ AuthService ←→ DB: users
//
//   NACH ERFOLGREICHEM LOGIN:
//     UserSession.CurrentUser = user (aus AuthService.Login())
//     → CurrentUserChanged-Event feuert
//     → MainViewModel.OnUserChanged() → navigiert zu DashboardVM
//     → ALLE anderen ViewModels (die CurrentUserChanged abonniert haben) laden neu
//
//   NACH REGISTRIERUNG:
//     Kein Auto-Login – User sieht Erfolgsmeldung und muss Login-Button klicken.
//
// USER USECASE:
//   1. App starten → LoginView erscheint (CurrentViewModel = LoginVM)
//   2. Username eingeben → Name-Setter → LoginCommand.RaiseCanExecuteChanged()
//   3. Passwort eingeben → Password-Setter → LoginCommand.RaiseCanExecuteChanged()
//   4. "Login"-Button wird aktiv (CanLoginOrRegister = true wenn beide nicht leer)
//   5. "Login" klicken → IsBusy = true → AuthService.Login() → Erfolg/Fehler
//   6. "Registrieren" klicken → AuthService.Register() → StatusMessage
//
// QUELLEN:
//   IsBusy-Pattern (Doppelklick-Schutz):
//   https://learn.microsoft.com/dotnet/architecture/maui/mvvm
//
//   PasswordBox: Passwort per Code-Behind übergeben (SecureString):
//   https://learn.microsoft.com/dotnet/desktop/wpf/controls/passwordbox
//
//   BCrypt.Net-Next (genutzt in AuthService):
//   https://github.com/BcryptNet/bcrypt.net
// ============================================================

using Smartpantry.Helpers;
using Smartpantry.Models;
using SmartPantry2.Services;
using System;

namespace Smartpantry.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        // AuthService: Login + Register + Profilverwaltung in der DB
        private readonly AuthService _authService = new AuthService();

        // Backing Fields für die Formular-Eingaben
        private string _username = "";
        private string _password = "";
        private string _email    = "";

        // ── FORMULAR-BINDINGS ─────────────────────────────────────────────────────
        // Jedes Textfeld in LoginView.xaml ist an eine dieser Properties gebunden.
        // Bei jeder Eingabe → RaiseCanExecuteChanged() → Button-Zustand aktualisieren.

        // Username-Eingabefeld → DB-Spalte "username"
        public string Username
        {
            get => _username;
            set
            {
                // SetProperty: schreibt Wert in _username und feuert PropertyChanged
                if (SetProperty(ref _username, value))
                    // Login- und Registrier-Button neu prüfen
                    // (aktiv nur wenn Username UND Password nicht leer)
                    LoginCommand.RaiseCanExecuteChanged();
            }
        }

        // Password-Eingabefeld (HINWEIS: PasswordBox in WPF unterstützt kein Binding,
        // daher wird das Passwort per Code-Behind übergeben oder per Attached Property)
        // Quelle: https://learn.microsoft.com/dotnet/desktop/wpf/controls/passwordbox
        public string Password
        {
            get => _password;
            set
            {
                if (SetProperty(ref _password, value))
                    LoginCommand.RaiseCanExecuteChanged();
            }
        }

        // E-Mail-Eingabe (nur bei Registrierung genutzt)
        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        // ── STATUS-ANZEIGE ────────────────────────────────────────────────────────
        // StatusMessage wird in LoginView.xaml angezeigt wenn Login/Register fehlschlägt.
        private string _statusMessage = "";
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        // ── LADEZUSTAND (Doppelklick-Schutz) ─────────────────────────────────────
        // IsBusy = true während DB-Abfrage läuft → Buttons werden deaktiviert.
        // Verhindert dass User mehrfach klickt während Login noch läuft.
        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    // Buttons neu prüfen: wenn IsBusy=true → beide inaktiv
                    LoginCommand.RaiseCanExecuteChanged();
                    RegisterCommand.RaiseCanExecuteChanged();
                }
            }
        }

        // ── COMMANDS (Button-Bindungen) ───────────────────────────────────────────
        // Quelle ICommand: https://learn.microsoft.com/dotnet/api/system.windows.input.icommand

        // Gebunden an: <Button Command="{Binding LoginCommand}" Content="Login"/>
        public RelayCommand LoginCommand { get; }

        // Gebunden an: <Button Command="{Binding RegisterCommand}" Content="Registrieren"/>
        public RelayCommand RegisterCommand { get; }

        public LoginViewModel()
        {
            // Login-Command: aktiv wenn nicht beschäftigt UND Felder nicht leer
            LoginCommand = new RelayCommand(Login, CanLoginOrRegister);

            // Register-Command: gleiche Bedingung
            RegisterCommand = new RelayCommand(Register, CanLoginOrRegister);
        }

        // --------------------------------------------------------
        // CanLoginOrRegister
        //
        // RETURN:
        //   true  → Login-/Register-Button ist aktiv (klickbar)
        //   false → Button ist ausgegraut
        //
        // BEDINGUNGEN:
        //   - Nicht gerade eine Anfrage läuft (!IsBusy)
        //   - Username ist nicht leer
        //   - Password ist nicht leer
        //
        // string.IsNullOrWhiteSpace: prüft auch auf nur-Leerzeichen
        // Quelle: https://learn.microsoft.com/dotnet/api/system.string.isnullorwhitespace
        // --------------------------------------------------------
        private bool CanLoginOrRegister() =>
            !IsBusy &&
            !string.IsNullOrWhiteSpace(Username) &&
            !string.IsNullOrWhiteSpace(Password);

        // --------------------------------------------------------
        // Login
        //
        // FUNKTION:
        //   Sendet Login-Anfrage an AuthService und setzt bei Erfolg
        //   UserSession.CurrentUser → triggert Navigation zu Dashboard.
        //
        // FLOW:
        //   IsBusy = true  → Buttons deaktivieren
        //   StatusMessage = "" → alte Fehlermeldung löschen
        //   AuthService.Login(Username, Password)
        //     → gibt User zurück wenn Credentials stimmen, sonst null
        //   null → StatusMessage = "Falscher Benutzername oder Passwort."
        //   User → UserSession.CurrentUser = user
        //         → CurrentUserChanged feuert
        //         → MainViewModel navigiert zu DashboardVM
        //   IsBusy = false → Buttons wieder aktiv
        //   catch: Unerwarteter Fehler → StatusMessage zeigen
        //
        // AUFGERUFEN VON: LoginCommand (Button-Klick in LoginView)
        // --------------------------------------------------------
        private void Login()
        {
            try
            {
                // Ladezustand einschalten → Doppelklick-Schutz
                IsBusy = true;
                // Alte Fehlermeldung löschen
                StatusMessage = "";

                // AuthService prüft Credentials in der DB
                // Gibt User (mit Settings per Include()) zurück oder null
                var user = _authService.Login(Username.Trim(), Password);

                if (user == null)
                {
                    // Fehlermeldung anzeigen (sichtbar über StatusMessage-Binding in LoginView)
                    StatusMessage = "Falscher Benutzername oder Passwort.";
                    return;
                }

                // Erfolgreich! User in Session speichern.
                // → UserSession.CurrentUserChanged feuert
                // → MainViewModel.OnUserChanged() navigiert zu Dashboard
                // → Alle abonnierten ViewModels (FoodVM, RecipesVM usw.) laden ihre Daten
                UserSession.CurrentUser = user;

                // Formular leeren für nächsten Login
                Username = "";
                Password = "";
            }
            catch (Exception ex)
            {
                // Unerwarteter Fehler (z.B. DB nicht erreichbar)
                StatusMessage = "Fehler beim Login: " + ex.Message;
            }
            finally
            {
                // Immer ausführen (auch bei Exception): Ladezustand aufheben
                // "finally" = wird immer ausgeführt, auch wenn return oder throw
                // Quelle: https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/try-finally
                IsBusy = false;
            }
        }

        // --------------------------------------------------------
        // Register
        //
        // FUNKTION:
        //   Legt neuen User in der DB an und zeigt Erfolgsmeldung.
        //   Kein Auto-Login: User muss danach selbst Login klicken.
        //
        // FLOW:
        //   Neues User-Objekt erstellen (Username, Email, Role="standard")
        //   AuthService.Register(user, Password)
        //     → true  → "Registrierung erfolgreich!" + Passwort-Feld leeren
        //     → false → "Benutzername existiert bereits."
        //
        // AUFGERUFEN VON: RegisterCommand (Button-Klick in LoginView)
        // --------------------------------------------------------
        private void Register()
        {
            try
            {
                IsBusy = true;
                StatusMessage = "";

                // Neues User-Objekt vorbereiten
                // PasswordHash wird von AuthService.Register() gesetzt (BCrypt)
                var newUser = new User
                {
                    Username = Username.Trim(),
                    Email    = Email.Trim(),
                    // Standard-Rolle: kein Admin bei Selbst-Registrierung
                    Role     = "standard"
                };

                // AuthService: Hash erstellen + in DB schreiben + Standard-Settings anlegen
                bool success = _authService.Register(newUser, Password);

                if (success)
                {
                    StatusMessage = "Registrierung erfolgreich! Bitte einloggen.";
                    // Passwort-Feld leeren (Username bleibt für schnellen Login)
                    Password = "";
                }
                else
                {
                    // AuthService hat Any()-Check gemacht: Username bereits vergeben
                    StatusMessage = "Benutzername existiert bereits.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "Fehler bei der Registrierung: " + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}