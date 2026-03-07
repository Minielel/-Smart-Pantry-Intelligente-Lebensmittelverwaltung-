// ------------------------------------------------------------
// Datei: FoodService.cs
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
using System.Collections.Generic;
using System.Linq;

namespace SmartPantry2.Services
{


    public class FoodService
    {
        // In diesem Service stehen alle wichtigen Datenbankaktionen fuer Lebensmittel.
        // Dadurch bleibt die eigentliche Benutzeroberflaeche schlanker und besser lesbar.


        public static event Action? FoodChanged;
        private static bool _isRaisingFoodChanged;



        public static void RaiseFoodChanged()
        {
            if (_isRaisingFoodChanged) return;

            try
            {
                _isRaisingFoodChanged = true;
                FoodChanged?.Invoke();
            }
            finally
            {
                _isRaisingFoodChanged = false;
            }
        }




        public List<FoodItem> GetAll()
        {
            using var db = new FoodDbContext();
            return db.FoodItems
                .Include(f => f.Category)
                .OrderBy(f => f.ExpiryDate)
                .ToList();
        }





        public void Add(FoodItem item)
        {
            using var db = new FoodDbContext();

            var normalizedName = (item.Name ?? "").Trim().ToLower();
            var normalizedUnit = (item.Unit ?? "").Trim().ToLower();
            var expiryDate = item.ExpiryDate.Date;



            var existing = db.FoodItems
                .AsEnumerable()
                .FirstOrDefault(f =>
                    f.UserId == item.UserId &&
                    ((f.Name ?? "").Trim().ToLower() == normalizedName) &&
                    ((f.Unit ?? "").Trim().ToLower() == normalizedUnit) &&
                    f.ExpiryDate.Date == expiryDate);

            if (existing == null)
            {
                db.FoodItems.Add(item);
            }
            else
            {
                existing.Amount += item.Amount;
                existing.CategoryId ??= item.CategoryId;
                if (item.CreatedAt != default)
                    existing.CreatedAt = item.CreatedAt;
            }

            db.SaveChanges();
            RaiseFoodChanged();
        }


        public void Update(FoodItem item)
        {
            using var db = new FoodDbContext();
            db.FoodItems.Update(item);
            db.SaveChanges();
            RaiseFoodChanged();
        }



        public void Delete(int id)
        {
            using var db = new FoodDbContext();

            var item = db.FoodItems.Find(id);
            if (item == null) return;

            db.FoodItems.Remove(item);
            db.SaveChanges();
            RaiseFoodChanged();
        }



        public List<FoodItem> GetExpiringSoon(int days = 3)
        {
            using var db = new FoodDbContext();

            var limit = DateTime.Today.AddDays(days);

            return db.FoodItems
                .Where(f => f.ExpiryDate <= limit)
                .ToList();
        }
    }
}
