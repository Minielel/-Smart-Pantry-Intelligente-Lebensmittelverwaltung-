// ------------------------------------------------------------
// Datei: SettingsService.cs
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
using SmartPantry2.Data;
using System.Linq;

namespace SmartPantry2.Services
{
    public class SettingsService
    {
        public UserSettings? Get(int userId)
        {
            using var db = new FoodDbContext();
            return db.UserSettings.FirstOrDefault(s => s.UserId == userId);
        }

        public void Add(UserSettings settings)
        {
            using var db = new FoodDbContext();
            db.UserSettings.Add(settings);
            db.SaveChanges();
        }

        public void Update(UserSettings settings)
        {
            using var db = new FoodDbContext();
            db.UserSettings.Update(settings);
            db.SaveChanges();
        }
    }
}
