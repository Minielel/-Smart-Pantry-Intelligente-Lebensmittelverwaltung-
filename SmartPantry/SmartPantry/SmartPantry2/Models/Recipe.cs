// ============================================================
// Datei:   Recipe.cs
// Schicht: Model / Datenmodell
// Datenbanktabelle: recipes  +  recipe_ingredients
//
// ZWECK:
//   Repräsentiert ein vollständiges Rezept mit Name, Beschreibung,
//   Anleitung und einer Liste von Zutaten.
//
// ROTER FADEN:
//   users              ──→ recipes (1:n, über user_id)
//   recipes            ──→ recipe_ingredients (1:n, über recipe_id)
//   recipes            ──→ meal_plan (1:n, über recipe_id)
//
//   RecipeService.GetAll() lädt Rezepte inkl. Zutaten:
//     .Include(r => r.Ingredients)
//   → Ingredients-Liste ist nach dem Laden gefüllt
//
// WICHTIG:
//   ImagePath ist [NotMapped] → wird NICHT in der Datenbank gespeichert!
//   Der Bildpfad existiert nur im Arbeitsspeicher (wird bei App-Neustart zurückgesetzt).
//   Für dauerhafte Bildspeicherung müsste die DB-Tabelle erweitert werden.
//
// USER USECASE:
//   Admin öffnet "Rezepte" → klickt "+"
//   → NewRecipe() leert das Formular
//   → Admin füllt Name, Beschreibung, Anleitung aus
//   → Admin fügt Zutaten hinzu
//   → Admin klickt "Add" → RecipeService.Add()
//   → Datensätze in recipes + recipe_ingredients
//
// QUELLEN:
//   [NotMapped] Data Annotation (EF Core):
//   https://learn.microsoft.com/ef/core/modeling/entity-properties#excluded-properties
//
//   System.ComponentModel.DataAnnotations:
//   https://learn.microsoft.com/dotnet/api/system.componentmodel.dataannotations
//
//   EF Core – One-to-Many Relationships:
//   https://learn.microsoft.com/ef/core/modeling/relationships/one-to-many
// ============================================================

// Für [NotMapped]-Attribut:
using System.ComponentModel.DataAnnotations.Schema;
using System;
using System.Collections.Generic;

namespace Smartpantry.Models
{
    public class Recipe
    {
        // Primärschlüssel → DB-Spalte "id"
        public int Id { get; set; }

        // Fremdschlüssel zu users.id → DB-Spalte "user_id"
        public int UserId { get; set; }

        // Rezeptname (z.B. "Spaghetti Bolognese") → DB-Spalte "name"
        public string Name { get; set; }

        // Kurzbeschreibung des Rezepts → DB-Spalte "description" (TEXT)
        public string Description { get; set; }

        // Schritt-für-Schritt-Anleitung → DB-Spalte "instructions" (TEXT)
        public string Instructions { get; set; }

        // Erstellungszeitpunkt → DB-Spalte "created_at"
        public DateTime CreatedAt { get; set; }

        // Navigationsproperty: der zugehörige User
        public User User { get; set; }

        // Navigationsproperty: alle Zutaten dieses Rezepts
        // Wird von RecipeService.GetAll() per Include(r => r.Ingredients) geladen
        // Quelle: https://learn.microsoft.com/ef/core/querying/related-data/eager
        public ICollection<RecipeIngredient> Ingredients { get; set; }

        // Navigationsproperty: alle Wochenplan-Einträge die dieses Rezept nutzen
        public ICollection<MealPlan> MealPlans { get; set; }

        // [NotMapped]: EF Core ignoriert diese Property komplett!
        // → KEIN Datenbankfeld dafür
        // → Wird nur im RAM gehalten während die App läuft
        // → Beim App-Neustart ist ImagePath immer null (nicht persistent)
        // Quelle: https://learn.microsoft.com/ef/core/modeling/entity-properties#excluded-properties
        [NotMapped]
        public string? ImagePath { get; set; }
    }
}