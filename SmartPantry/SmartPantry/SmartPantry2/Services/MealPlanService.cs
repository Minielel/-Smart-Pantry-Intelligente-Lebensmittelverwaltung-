// ============================================================
// Datei:   MealPlanService.cs
// Schicht: Service / Wochenplanung
//
// ZWECK:
//   CRUD-Operationen für den Wochenplan.
//   GetWeekPlan() lädt Einträge inkl. Rezeptname per Include().
//
// ROTER FADEN:
//   MealPlanViewModel → MealPlanService → DB: meal_plan + recipes
//
// QUELLEN:
//   EF Core – Include() Eager Loading:
//   https://learn.microsoft.com/ef/core/querying/related-data/eager
//
//   EF Core – Find() (PK-Suche):
//   https://learn.microsoft.com/ef/core/querying/tracking#find-and-findasync
// ============================================================

using Microsoft.EntityFrameworkCore;
using Smartpantry.Models;
using SmartPantry2.Data;
using System.Collections.Generic;
using System.Linq;

namespace SmartPantry2.Services
{
    public class MealPlanService
    {
        // --------------------------------------------------------
        // GetWeekPlan
        //
        // FUNKTION:
        //   Lädt alle Wochenplan-Einträge eines Users aus der DB.
        //   Lädt das verknüpfte Rezept per Include() mit,
        //   da Recipe.Name in MealPlanView.xaml angezeigt wird.
        //
        // RETURN: List<MealPlan> mit befüllter Recipe-Navigation
        //   → Recipe.Name für Anzeige "<TextBlock Text="{Binding Recipe.Name}"/>"
        //
        // DB-Zugriff:
        //   SELECT mp.*, r.* FROM meal_plan mp
        //   INNER JOIN recipes r ON mp.recipe_id = r.id
        //   WHERE mp.user_id = ?
        //
        // AUFGERUFEN VON: MealPlanViewModel.Load()
        // --------------------------------------------------------
        public List<MealPlan> GetWeekPlan(int userId)
        {
            using var db = new FoodDbContext();

            return db.MealPlans
                // Include: Recipe-Navigation laden damit Recipe.Name verfügbar ist
                // Quelle: https://learn.microsoft.com/ef/core/querying/related-data/eager
                .Include(m => m.Recipe)
                // Nur Einträge des angegebenen Users
                .Where(m => m.UserId == userId)
                .ToList();
        }

        // --------------------------------------------------------
        // Add
        //
        // FUNKTION: Fügt neuen Wochenplan-Eintrag in die DB ein
        //
        // DB: INSERT INTO meal_plan (user_id, recipe_id, date, meal_type)
        // AUFGERUFEN VON: MealPlanViewModel.AddPlan()
        // --------------------------------------------------------
        public void Add(MealPlan plan)
        {
            using var db = new FoodDbContext();
            db.MealPlans.Add(plan);
            db.SaveChanges();
        }

        // --------------------------------------------------------
        // Remove
        //
        // FUNKTION: Löscht einen Wochenplan-Eintrag per Id
        //
        // DB: DELETE FROM meal_plan WHERE id=?
        // AUFGERUFEN VON: MealPlanViewModel.RemovePlan()
        // --------------------------------------------------------
        public void Remove(int id)
        {
            using var db = new FoodDbContext();

            // Find(): PK-Index-Suche – optimal für ID-Lookups
            var entry = db.MealPlans.Find(id);
            // Defensiv: wenn nicht gefunden → nichts tun
            if (entry == null) return;

            db.MealPlans.Remove(entry);
            db.SaveChanges();
        }
    }
}