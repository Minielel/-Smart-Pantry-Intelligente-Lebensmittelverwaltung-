// ------------------------------------------------------------
// Datei: DashboardService.cs
//
// Beschreibung:
// Diese Datei gehört zur Service-Schicht. Hier werden Aufgaben wie Datenbankzugriffe, Hilfslogik oder das Nachladen von Daten erledigt.
//
// Hinweis fuer die Vorstellung:
// Wenn man diese Datei in der Schule erklaeren moechte, kann man sagen,
// dass sie einen bestimmten Baustein der App uebernimmt und dadurch hilft,
// die Anwendung klar zu strukturieren.
// ------------------------------------------------------------
﻿using SmartPantry2.Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace SmartPantry2.Services
{
    public class DashboardService
    {
        public (int total, int expiringSoon, int expired, int recipes) GetStats(int? userId = null)
        {
            using var db = new FoodDbContext();

            var food = db.FoodItems.AsQueryable();
            var recipesQ = db.Recipes.AsQueryable();

            if (userId.HasValue)
            {
                food = food.Where(f => f.UserId == userId.Value);
                recipesQ = recipesQ.Where(r => r.UserId == userId.Value);
            }

            int total = food.Count();
            int expired = food.Count(f => f.ExpiryDate < DateTime.Today);
            int expiringSoon = food.Count(f => f.ExpiryDate >= DateTime.Today && f.ExpiryDate <= DateTime.Today.AddDays(3));
            int recipes = recipesQ.Count();

            return (total, expiringSoon, expired, recipes);
        }
    }
}
