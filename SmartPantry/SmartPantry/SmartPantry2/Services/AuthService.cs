// ------------------------------------------------------------
// Datei: AuthService.cs
//
// Beschreibung:
// Diese Datei gehört zur Service-Schicht. Hier werden Aufgaben wie Datenbankzugriffe, Hilfslogik oder das Nachladen von Daten erledigt.
//
// Hinweis fuer die Vorstellung:
// Wenn man diese Datei in der Schule erklaeren moechte, kann man sagen,
// dass sie einen bestimmten Baustein der App uebernimmt und dadurch hilft,
// die Anwendung klar zu strukturieren.
// ------------------------------------------------------------
using Microsoft.EntityFrameworkCore;
using Smartpantry.Models;
using SmartPantry2.Data;
using System;
using System.Linq;

namespace SmartPantry2.Services
{
    public class AuthService
    {
        public User? Login(string username, string password)
        {
            using var db = new FoodDbContext();

            var user = db.Users
                .Include(u => u.Settings)
                .FirstOrDefault(u => u.Username == username);

            if (user == null)
                return null;

            bool valid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
            return valid ? user : null;
        }

        public bool Register(User user, string password)
        {
            using var db = new FoodDbContext();

            if (db.Users.Any(u => u.Username == user.Username))
                return false;

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
            user.CreatedAt = DateTime.Now;

            db.Users.Add(user);
            db.SaveChanges();


            if (user.Settings == null)
            {
                db.UserSettings.Add(new UserSettings
                {
                    UserId = user.Id,
                    Theme = "green",
                    Language = "de"
                });
                db.SaveChanges();
            }

            return true;
        }

        public bool UpdateProfile(int userId, string username, string email, string role)
        {
            using var db = new FoodDbContext();
            var user = db.Users.FirstOrDefault(u => u.Id == userId);
            if (user == null) return false;


            if (!string.Equals(user.Username, username, StringComparison.OrdinalIgnoreCase)
                && db.Users.Any(u => u.Username == username && u.Id != userId))
                return false;

            user.Username = username;
            user.Email = email;
            user.Role = role;
            db.SaveChanges();
            return true;
        }

        public bool ChangePassword(int userId, string currentPassword, string newPassword)
        {
            using var db = new FoodDbContext();
            var user = db.Users.FirstOrDefault(u => u.Id == userId);
            if (user == null) return false;

            if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
                return false;

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            db.SaveChanges();
            return true;
        }
    }
}
