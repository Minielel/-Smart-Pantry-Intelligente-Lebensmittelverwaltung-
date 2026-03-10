// ============================================================
// Datei:   User.cs
// Schicht: Model / Datenmodell
// Datenbanktabelle: users
//
// ZWECK:
//   Repräsentiert einen Benutzer der SmartPantry-App.
//   ZENTRALE Entität: alle anderen Haupttabellen haben einen
//   Fremdschlüssel auf users.id.
//
// ROTER FADEN:
//   users ist die Wurzel des gesamten Datenschemas:
//   users ──┬──→ user_settings  (1:1)
//           ├──→ food_items      (1:n)
//           ├──→ recipes         (1:n)
//           ├──→ meal_plan       (1:n)
//           └──→ shopping_list   (1:n)
//
//   Nach erfolgreichem Login:
//     AuthService.Login() → gibt User zurück
//     LoginViewModel setzt UserSession.CurrentUser = user
//     → alle Services filtern ihre Queries per UserId
//     → jeder User sieht nur seine eigenen Daten
//
//   Role bestimmt Berechtigungen:
//     "admin"    → CanEdit = true  → darf hinzufügen, bearbeiten, löschen
//     "standard" → CanEdit = false → nur Lesezugriff
//
// USER USECASE:
//   Registrierung: AuthService.Register() → INSERT INTO users
//   Login:         AuthService.Login()    → SELECT * FROM users WHERE username=?
//   Logout:        UserSession.Logout()   → CurrentUser = null (nur RAM, kein DB)
//
// QUELLEN:
//   BCrypt.Net-Next (Passwort-Hashing, kein Klartext in DB!):
//   https://github.com/BcryptNet/bcrypt.net
//
//   EF Core – Primary Keys:
//   https://learn.microsoft.com/ef/core/modeling/keys
//
//   EF Core – Navigation Properties:
//   https://learn.microsoft.com/ef/core/modeling/relationships
// ============================================================

namespace Smartpantry.Models
{
    public class User
    {
        // Primärschlüssel → DB-Spalte "id" (AUTO_INCREMENT)
        // EF Core erkennt "Id" automatisch als PK
        public int Id { get; set; }

        // Login-Name des Users → DB-Spalte "username" (UNIQUE in DB)
        // Wird für Login-Prüfung in AuthService genutzt
        public string Username { get; set; }

        // E-Mail-Adresse → DB-Spalte "email" (UNIQUE, kann null sein)
        public string Email { get; set; }

        // BCrypt-Hash des Passworts (NIEMALS Klartext!)
        // Beispiel-Hash: "$2a$11$xyz..." (60 Zeichen)
        // AuthService.Login() prüft mit: BCrypt.Verify(plaintext, PasswordHash)
        // Quelle: https://github.com/BcryptNet/bcrypt.net
        // DB-Spalte: "password_hash"
        public string PasswordHash { get; set; }

        // Rolle: "admin" oder "standard"
        // DB: ENUM('admin','standard') DEFAULT 'standard'
        // Wird von UserSession.IsAdmin ausgewertet:
        //   public static bool IsAdmin => Role == "admin"
        // DB-Spalte: "role"
        public string Role { get; set; }

        // Registrierungszeitpunkt → DB-Spalte "created_at"
        // Wird in AuthService.Register() auf DateTime.Now gesetzt
        public DateTime CreatedAt { get; set; }

        // Navigationsproperty: die UserSettings (1:1)
        // Wird von AuthService.Login() per Include(u => u.Settings) geladen
        // → Theme und Sprache sofort nach Login verfügbar
        public UserSettings Settings { get; set; }

        // Navigationsproperty: alle Lebensmittel dieses Users (1:n)
        public ICollection<FoodItem> FoodItems { get; set; }

        // Navigationsproperty: alle Rezepte dieses Users (1:n)
        public ICollection<Recipe> Recipes { get; set; }

        // Navigationsproperty: alle Wochenplan-Einträge dieses Users (1:n)
        public ICollection<MealPlan> MealPlans { get; set; }

        // Navigationsproperty: die Einkaufsliste des Users (1:n)
        public ICollection<ShoppingItem> ShoppingList { get; set; }
    }
}