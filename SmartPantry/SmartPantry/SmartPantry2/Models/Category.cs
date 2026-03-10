// ============================================================
// Datei:   Category.cs
// Schicht: Model / Datenmodell
// Datenbanktabelle: categories
//
// ZWECK:
//   Repräsentiert eine Lebensmittelkategorie.
//   Jedes FoodItem kann optional einer Kategorie zugeordnet werden.
//
// ROTER FADEN:
//   categories ──→ food_items (1:n, über food_items.category_id)
//   Wenn Kategorie gelöscht → food_items.category_id wird NULL
//   (ON DELETE SET NULL im SQL-Schema)
//
//   Wird gemappt in FoodDbContext:
//     e.ToTable("categories");
//     e.Property(p => p.Id).HasColumnName("id");
//     e.Property(p => p.Name).HasColumnName("name");
//
// USER USECASE:
//   Aktuell kein direktes UI für Kategorien vorhanden.
//   Tabelle ist für zukünftige Erweiterungen vorbereitet
//   (z.B. Filteransicht nach Kategorie in FoodView).
//
// QUELLEN:
//   Entity Framework Core – Modellierung:
//   https://learn.microsoft.com/ef/core/modeling/
//
//   EF Core Navigation Properties:
//   https://learn.microsoft.com/ef/core/modeling/relationships
// ============================================================

namespace Smartpantry.Models
{
    public class Category
    {
        // Primärschlüssel → DB-Spalte "id" (gemappt in FoodDbContext)
        // EF Core erkennt "Id" automatisch als PK (Convention)
        // Quelle: https://learn.microsoft.com/ef/core/modeling/keys
        public int Id { get; set; }

        // Name der Kategorie (z.B. "Gemüse", "Milchprodukte", "Getränke")
        // DB-Spalte: "name"
        public string Name { get; set; }

        // Navigationsproperty: alle FoodItems dieser Kategorie
        // EF Core nutzt dies für JOIN-Abfragen wenn Include() verwendet wird
        // ICollection = Interface für jede Art von Sammlung (List, HashSet etc.)
        // Quelle: https://learn.microsoft.com/ef/core/modeling/relationships/one-to-many
        public ICollection<FoodItem> FoodItems { get; set; }
    }
}