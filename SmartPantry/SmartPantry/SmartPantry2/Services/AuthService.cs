// ============================================================
// Datei:   AuthService.cs
// Schicht: Service / Authentifizierung
//
// ZWECK:
//   Alle Datenbankoperationen für Benutzer-Authentifizierung.
//   Login, Registrierung, Profilupdate, Passwortänderung.
//   Nutzt BCrypt für sichere Passwort-Hashes.
//
// ROTER FADEN:
//   LoginViewModel.Login()             → AuthService.Login()
//   LoginViewModel.Register()          → AuthService.Register()
//   SettingsViewModel.SaveProfile()    → AuthService.UpdateProfile()
//   SettingsViewModel.ChangePassword() → AuthService.ChangePassword()
//   Alle Methoden: DB-Tabelle "users" (+ "user_settings" bei Register)
//
// QUELLEN:
//   BCrypt.Net-Next (Passwort-Hashing):
//   https://github.com/BcryptNet/bcrypt.net
//   Dokumentation BCrypt.Verify / HashPassword:
//   https://github.com/BcryptNet/bcrypt.net#usage
//
//   Entity Framework Core – Include() (Eager Loading):
//   https://learn.microsoft.com/ef/core/querying/related-data/eager
//
//   EF Core – FirstOrDefault():
//   https://learn.microsoft.com/dotnet/api/system.linq.queryable.firstordefault
//
//   LINQ – Any() (Existenzprüfung):
//   https://learn.microsoft.com/dotnet/api/system.linq.queryable.any
// ============================================================

using Microsoft.EntityFrameworkCore;
using Smartpantry.Models;
using SmartPantry2.Data;
using System;
using System.Linq;

namespace SmartPantry2.Services
{
    public class AuthService
    {
        // --------------------------------------------------------
        // Login
        //
        // FUNKTION:
        //   Sucht den User per Username in der DB.
        //   Prüft das Passwort mit BCrypt.Verify().
        //   Lädt die UserSettings per Include() direkt mit.
        //
        // PARAMETER:
        //   username → eingegebener Username aus LoginView (Textfeld)
        //   password → eingegebenes Passwort als KLARTEXT (wird NICHT gespeichert)
        //
        // RETURN:
        //   User-Objekt (mit geladenen Settings) → wird in UserSession gesetzt
        //   null → Username nicht gefunden ODER Passwort falsch
        //
        // DB-Zugriff:
        //   SELECT users.*, user_settings.*
        //   FROM users
        //   LEFT JOIN user_settings ON user_settings.user_id = users.id
        //   WHERE users.username = ?
        //   LIMIT 1
        //
        // DANACH IN LoginViewModel:
        //   if (user != null) UserSession.CurrentUser = user;
        // --------------------------------------------------------
        public User? Login(string username, string password)
        {
            // "using var" = DbContext wird nach dem Block automatisch disposed
            // Quelle: https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/using-statement
            using var db = new FoodDbContext();

            // Include(u => u.Settings): lädt UserSettings per JOIN mit
            // → Theme und Sprache sind sofort nach dem Login verfügbar
            // → SettingsViewModel.Load() kann sie direkt anwenden
            // Quelle: https://learn.microsoft.com/ef/core/querying/related-data/eager
            var user = db.Users
                .Include(u => u.Settings)
                .FirstOrDefault(u => u.Username == username);

            // Username nicht in DB gefunden → null zurückgeben
            if (user == null)
                return null;

            // BCrypt.Verify: prüft Klartext-Passwort gegen den gespeicherten Hash
            // SICHER: das Passwort wird nie im Klartext in die DB geschrieben
            // Quelle: https://github.com/BcryptNet/bcrypt.net#usage
            bool valid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);

            // Ternärer Operator: wenn gültig → User zurückgeben, sonst null
            return valid ? user : null;
        }

        // --------------------------------------------------------
        // Register
        //
        // FUNKTION:
        //   Legt neuen User in der DB an und erstellt automatisch
        //   Standard-UserSettings (Theme=green, Sprache=de).
        //
        // PARAMETER:
        //   user     → User-Objekt mit Username, Email, Role
        //              (PasswordHash wird HIER gesetzt, nicht vorher!)
        //   password → Klartext-Passwort → wird zu BCrypt-Hash
        //
        // RETURN:
        //   true  → Registrierung erfolgreich
        //   false → Username existiert bereits (UNIQUE-Verletzung verhindert)
        //
        // DB-Zugriff:
        //   1. SELECT COUNT(*) FROM users WHERE username = ? → Duplikat-Prüfung
        //   2. INSERT INTO users (username, email, password_hash, role, created_at)
        //   3. INSERT INTO user_settings (user_id, theme, language)
        //
        // DANACH: User muss sich separat einloggen (kein Auto-Login nach Register)
        // --------------------------------------------------------
        public bool Register(User user, string password)
        {
            using var db = new FoodDbContext();

            // Any() prüft ob mindestens ein Datensatz mit gleichem Username existiert
            // → verhindert doppelte Usernames (DB hat UNIQUE-Constraint als Sicherheitsnetz)
            // Quelle: https://learn.microsoft.com/dotnet/api/system.linq.queryable.any
            if (db.Users.Any(u => u.Username == user.Username))
                return false;

            // BCrypt.HashPassword: erzeugt sicheren Hash aus Klartext-Passwort
            // "11" = Cost-Factor (Standard): je höher, desto sicherer aber langsamer
            // Quelle: https://github.com/BcryptNet/bcrypt.net#usage
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);

            // Erstellungszeitpunkt setzen
            user.CreatedAt = DateTime.Now;

            // User zur DB hinzufügen (noch kein SaveChanges → noch nicht in DB)
            db.Users.Add(user);

            // Änderungen in DB schreiben → INSERT INTO users (...)
            // Nach SaveChanges() ist user.Id von der DB befüllt (AUTO_INCREMENT)
            // Quelle: https://learn.microsoft.com/ef/core/saving/basic
            db.SaveChanges();

            // Wenn noch keine Settings vorhanden → Standard-Settings anlegen
            // (Normalfall bei Neuregistrierung)
            if (user.Settings == null)
            {
                // Standard-Einstellungen: grünes Theme, Deutsch
                db.UserSettings.Add(new UserSettings
                {
                    // user.Id ist jetzt bekannt (von DB nach SaveChanges befüllt)
                    UserId = user.Id,
                    Theme = "green",
                    Language = "de"
                });
                // Zweites SaveChanges für die Settings
                db.SaveChanges();
            }

            return true;
        }

        // --------------------------------------------------------
        // UpdateProfile
        //
        // FUNKTION:
        //   Ändert Username, Email und Rolle eines bestehenden Users.
        //
        // PARAMETER:
        //   userId   → Id des zu ändernden Users (aus UserSession)
        //   username → neuer Username
        //   email    → neue E-Mail
        //   role     → neue Rolle ("admin" oder "standard")
        //
        // RETURN:
        //   true  → Profil gespeichert
        //   false → User nicht gefunden
        //           ODER neuer Username ist bereits von anderem User belegt
        //
        // DB-Zugriff:
        //   UPDATE users SET username=?, email=?, role=? WHERE id=?
        // --------------------------------------------------------
        public bool UpdateProfile(int userId, string username, string email, string role)
        {
            using var db = new FoodDbContext();
            var user = db.Users.FirstOrDefault(u => u.Id == userId);
            if (user == null) return false;

            // Prüfen ob der neue Username bereits von einem ANDEREN User verwendet wird
            // OrdinalIgnoreCase: "Admin" und "admin" gelten als gleich
            // "u.Id != userId" = der User selbst darf seinen Namen behalten
            if (!string.Equals(user.Username, username, StringComparison.OrdinalIgnoreCase)
                && db.Users.Any(u => u.Username == username && u.Id != userId))
                return false;

            // Werte setzen – EF Core "tracked" die Entity und erkennt die Änderungen
            user.Username = username;
            user.Email = email;
            user.Role = role;

            // UPDATE-SQL wird ausgeführt
            db.SaveChanges();
            return true;
        }

        // --------------------------------------------------------
        // ChangePassword
        //
        // FUNKTION:
        //   Prüft das aktuelle Passwort und ersetzt es durch
        //   einen neuen BCrypt-Hash.
        //
        // RETURN:
        //   true  → Passwort erfolgreich geändert
        //   false → User nicht gefunden ODER aktuelles Passwort falsch
        //
        // DB-Zugriff:
        //   UPDATE users SET password_hash=? WHERE id=?
        // --------------------------------------------------------
        public bool ChangePassword(int userId, string currentPassword, string newPassword)
        {
            using var db = new FoodDbContext();
            var user = db.Users.FirstOrDefault(u => u.Id == userId);
            if (user == null) return false;

            // Sicherheitscheck: aktuelles Passwort muss stimmen
            // Verhindert dass jemand das Passwort ändern kann ohne das alte zu kennen
            if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
                return false;

            // Neuen Hash erzeugen und speichern
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            db.SaveChanges();
            return true;
        }
    }
}