// ============================================================
// Datei:   RecipeIngredient.cs
// Schicht: Model / Datenmodell
// Datenbanktabelle: recipe_ingredients
//
// ZWECK:
//   Eine Zutat innerhalb eines Rezepts.
//   Speichert Zutatennamen als Text (nicht als FK zu food_items).
//
// ROTER FADEN:
//   recipes ──→ recipe_ingredients (1:n, über recipe_id)
//   Wenn Rezept gelöscht → alle Zutaten per CASCADE mitgelöscht
//   (ON DELETE CASCADE im SQL-Schema)
//
// DESIGN-ENTSCHEIDUNG (warum kein FK zu food_items?):
//   FoodItem (Zutatenname als Text) ist KEIN Fremdschlüssel zu food_items!
//   Gründe:
//   1. Rezepte sollen unabhängig vom aktuellen Vorrat bleiben
//   2. Ein Rezept bleibt gültig auch wenn das Item nicht im Vorrat ist
//   3. Einfacheres Datenmodell ohne komplexe FK-Abhängigkeiten
//
//   WICHTIGER NAME-UNTERSCHIED:
//   C# Property "FoodItem" ↔ DB-Spalte "food_item_name"
//   Mapping in FoodDbContext:
//     e.Property(p => p.FoodItem).HasColumnName("food_item_name");
//
// USER USECASE:
//   Admin fügt im Rezept-Formular eine Zutat hinzu:
//   Option A: Textfeld manuell ausfüllen → NewIngredientName eingeben
//   Option B: "Zutat auswählen" → FoodView im Auswahlmodus →
//             Name des gewählten FoodItems wird übernommen
//   → "+" klicken → RecipeIngredient-Objekt wird erstellt
//   → RecipeService.Update() speichert es in recipe_ingredients
//
// QUELLEN:
//   EF Core – Owned Entity Types vs. simple string properties:
//   https://learn.microsoft.com/ef/core/modeling/owned-entities
//
//   EF Core – One-to-Many:
//   https://learn.microsoft.com/ef/core/modeling/relationships/one-to-many
// ============================================================

namespace Smartpantry.Models
{
    public class RecipeIngredient
    {
        // Primärschlüssel → DB-Spalte "id"
        public int Id { get; set; }

        // Fremdschlüssel zu recipes.id → DB-Spalte "recipe_id"
        // Bei Löschen des Rezepts: CASCADE → diese Zeile wird mitgelöscht
        public int RecipeId { get; set; }

        // Name der Zutat als PLAIN TEXT (kein FK zu food_items!)
        // Beispiel: "Mehl", "Eier", "Butter"
        // ACHTUNG: C# "FoodItem" ↔ DB "food_item_name" (Mapping in FoodDbContext!)
        // DB-Spalte: "food_item_name"
        public string FoodItem { get; set; }

        // Menge der Zutat (z.B. 200 für "200g Mehl")
        // decimal für präzise Dezimalzahlen → DB: DECIMAL(10,2)
        // DB-Spalte: "amount"
        public decimal Amount { get; set; }

        // Einheit (z.B. "g", "ml", "EL", "TL", "Stück", "Messerspitze")
        // DB-Spalte: "unit"
        public string Unit { get; set; }

        // Navigationsproperty: das zugehörige Rezept
        // Rückreferenz für EF Core Relationship-Tracking
        public Recipe Recipe { get; set; }
    }
}