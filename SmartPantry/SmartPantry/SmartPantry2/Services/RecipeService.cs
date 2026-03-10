// ============================================================
// Datei:   RecipeService.cs
// Schicht: Service / Rezeptverwaltung
//
// ZWECK:
//   CRUD-Operationen für Rezepte inkl. Zutaten.
//   GetAll() lädt Zutaten per Include() automatisch mit.
//
// ROTER FADEN:
//   RecipesViewModel → RecipeService → DB: recipes + recipe_ingredients
//
//   WICHTIG bei Delete():
//   recipe_ingredients werden per ON DELETE CASCADE automatisch
//   mitgelöscht wenn das Rezept gelöscht wird.
//   (Kein explizites Löschen der Zutaten nötig)
//
// QUELLEN:
//   EF Core – Include() (Eager Loading):
//   https://learn.microsoft.com/ef/core/querying/related-data/eager
//
//   EF Core – Update() (Disconnected Entities):
//   https://learn.microsoft.com/ef/core/saving/disconnected-entities
//
//   EF Core – Find():
//   https://learn.microsoft.com/ef/core/querying/tracking#find-and-findasync
// ============================================================

using Microsoft.EntityFrameworkCore;
using Smartpantry.Models;
using SmartPantry2.Data;
using System.Collections.Generic;

namespace SmartPantry2.Services
{
    public class RecipeService
    {
        // --------------------------------------------------------
        // GetAll
        //
        // FUNKTION: Lädt alle Rezepte mit ihren Zutaten aus der DB
        //
        // RETURN: List<Recipe> mit befüllter Ingredients-Liste
        //   → jedes Recipe.Ingredients enthält alle Zutaten
        //
        // DB-Zugriff:
        //   SELECT r.*, ri.* FROM recipes r
        //   LEFT JOIN recipe_ingredients ri ON ri.recipe_id = r.id
        //
        // AUFGERUFEN VON: RecipesViewModel.Load()
        //   → filtert danach noch nach UserId und IsAdmin
        // --------------------------------------------------------
        public List<Recipe> GetAll()
        {
            using var db = new FoodDbContext();

            return db.Recipes
                // Include: Zutaten-Liste per JOIN mitladen
                // → r.Ingredients ist danach nicht null
                .Include(r => r.Ingredients)
                .ToList();
        }

        // --------------------------------------------------------
        // Add
        //
        // FUNKTION: Fügt neues Rezept mit Zutaten in die DB ein
        //
        // EF Core erkennt die Ingredients-Liste automatisch und
        // führt sowohl INSERT INTO recipes als auch
        // INSERT INTO recipe_ingredients durch.
        //
        // DB:
        //   INSERT INTO recipes (user_id, name, description, instructions, created_at)
        //   + INSERT INTO recipe_ingredients (...) für jede Zutat
        // --------------------------------------------------------
        public void Add(Recipe recipe)
        {
            using var db = new FoodDbContext();
            // Add(): EF Core tracked das Recipe + alle Ingredients
            db.Recipes.Add(recipe);
            // SaveChanges(): führt alle INSERT-Statements aus
            db.SaveChanges();
        }

        // --------------------------------------------------------
        // Update
        //
        // FUNKTION: Aktualisiert ein bestehendes Rezept mit Zutaten
        //
        // Update() mit verschachtelten Entities:
        // EF Core markiert alle Änderungen am Recipe und seinen
        // Ingredients als "Modified" → UPDATE und INSERT/DELETE
        //
        // DB:
        //   UPDATE recipes SET name=?, description=?, instructions=? WHERE id=?
        //   + DELETE/INSERT für geänderte recipe_ingredients
        //
        // AUFGERUFEN VON: RecipesViewModel.AddRecipe() (wenn SelectedRecipe != null)
        //   und RecipesViewModel.AddIngredient() / RemoveIngredient()
        // --------------------------------------------------------
        public void Update(Recipe recipe)
        {
            using var db = new FoodDbContext();
            // Update(): teilt EF Core mit dass alle Properties geändert sein könnten
            // Quelle: https://learn.microsoft.com/ef/core/saving/disconnected-entities
            db.Recipes.Update(recipe);
            db.SaveChanges();
        }

        // --------------------------------------------------------
        // Delete
        //
        // FUNKTION: Löscht ein Rezept per Id
        //   recipe_ingredients werden per ON DELETE CASCADE automatisch mitgelöscht!
        //   meal_plan-Einträge die dieses Rezept referenzieren auch!
        //
        // DB:
        //   DELETE FROM recipes WHERE id=?
        //   → CASCADE: DELETE FROM recipe_ingredients WHERE recipe_id=?
        //   → CASCADE: DELETE FROM meal_plan WHERE recipe_id=?
        //
        // AUFGERUFEN VON: RecipesViewModel.DeleteRecipeTile()
        //   (mit vorheriger Bestätigungs-MessageBox)
        // --------------------------------------------------------
        public void Delete(int id)
        {
            using var db = new FoodDbContext();

            // Find(): effiziente PK-Suche
            var recipe = db.Recipes.Find(id);
            // Defensiv: wenn Rezept nicht gefunden → nichts tun
            if (recipe == null) return;

            db.Recipes.Remove(recipe);
            db.SaveChanges();
        }
    }
}