// ------------------------------------------------------------
// Datei: MealPlanService.cs
//
// Beschreibung:
// Diese Datei gehört zur Service-Schicht. Hier werden Aufgaben wie Datenbankzugriffe, Hilfslogik oder das Nachladen von Daten erledigt.
//
// Hinweis fuer die Vorstellung:
// Wenn man diese Datei in der Schule erklaeren moechte, kann man sagen,
// dass sie einen bestimmten Baustein der App uebernimmt und dadurch hilft,
// die Anwendung klar zu strukturieren.
// ------------------------------------------------------------
﻿using Microsoft.EntityFrameworkCore;
using Smartpantry.Models;
using SmartPantry2.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SmartPantry2.Services
{
    public class MealPlanService
    {
        public List<MealPlan> GetWeekPlan(int userId)
        {
            using var db = new FoodDbContext();

            return db.MealPlans
                .Include(m => m.Recipe)
                .Where(m => m.UserId == userId)
                .ToList();
        }

        public void Add(MealPlan plan)
        {
            using var db = new FoodDbContext();
            db.MealPlans.Add(plan);
            db.SaveChanges();
        }

        public void Remove(int id)
        {
            using var db = new FoodDbContext();

            var entry = db.MealPlans.Find(id);
            if (entry == null) return;

            db.MealPlans.Remove(entry);
            db.SaveChanges();
        }
    }
}
