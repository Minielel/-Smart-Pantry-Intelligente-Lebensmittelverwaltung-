// ============================================================
// Datei:   FoodItem.cs
// Schicht: Model / Datenmodell
// Datenbanktabelle: food_items
//
// ZWECK:
//   Repräsentiert ein Lebensmittel im Vorrat eines Benutzers.
//   Zentrale Entität der App – nahezu alle Features interagieren
//   direkt oder indirekt mit dieser Klasse.
//
// ROTER FADEN:
//   users     ──→ food_items (1:n, über user_id)
//   categories ──→ food_items (1:n, über category_id – nullable)
//
//   FoodItem wird verwendet von:
//   ├─ FoodService     → CRUD-Operationen (Add mit Merge-Logik, Update, Delete)
//   ├─ FoodViewModel   → Anzeige als Kacheln + Formular in FoodView.xaml
//   ├─ ShoppingService → MoveToFoodAndRemove(): Shopping-Item → Food-Item
//   └─ DashboardService → zählt Items, prüft Ablaufdaten für Warnfarben
//
//   WICHTIGER NAME-UNTERSCHIED:
//   C# Property "ExpiryDate" ↔ DB-Spalte "expiration_date"
//   Das Mapping steht in FoodDbContext.OnModelCreating():
//     e.Property(p => p.ExpiryDate).HasColumnName("expiration_date");
//
// USER USECASE:
//   Admin öffnet "Food/Pantry"
//   → FoodService.GetAll() → alle FoodItems aus DB laden
//   → FoodViewModel filtert nach UserId → nur eigene Items
//   → FoodView zeigt Items als Kacheln mit Name, Menge, Einheit
//   → Admin füllt Formular aus → Add() → neuer food_items-Datensatz
//
// QUELLEN:
//   EF Core – Data Annotations:
//   https://learn.microsoft.com/ef/core/modeling/entity-properties
//
//   EF Core – Nullable Foreign Keys:
//   https://learn.microsoft.com/ef/core/modeling/relationships/foreign-and-principal-keys
//
//   decimal für Geldbeträge/Mengen (Präzision):
//   https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/floating-point-numeric-types
// ============================================================

namespace Smartpantry.Models
{
    public class FoodItem
    {
        // Primärschlüssel → DB-Spalte "id"
        public int Id { get; set; }

        // Fremdschlüssel zu users.id
        // → damit FoodService nur Items des eingeloggten Users lädt
        // DB-Spalte: "user_id"
        public int UserId { get; set; }

        // Name des Lebensmittels (z.B. "Milch", "Mehl")
        // DB-Spalte: "name"
        public string Name { get; set; }

        // Menge (z.B. 500)
        // decimal statt double → keine Rundungsfehler bei Dezimalzahlen
        // DB-Spalte: "amount" (DECIMAL(10,2) in SQL)
        public decimal Amount { get; set; }

        // Einheit (z.B. "g", "ml", "Stück")
        // DB-Spalte: "unit"
        public string Unit { get; set; }

        // Ablaufdatum → wird für Dashboard-Warnfarben ausgewertet
        // ACHTUNG: C# "ExpiryDate" ↔ DB "expiration_date" (Mapping in FoodDbContext!)
        // DB-Spalte: "expiration_date"
        public DateTime ExpiryDate { get; set; }

        // Fremdschlüssel zur Kategorie – "int?" = nullable (Kategorie optional)
        // Wenn null → Item hat keine Kategorie
        // DB-Spalte: "category_id" (nullable)
        public int? CategoryId { get; set; }

        // Erstellungszeitpunkt des Datensatzes
        // DB-Spalte: "created_at"
        public DateTime CreatedAt { get; set; }

        // Navigationsproperty: das zugehörige User-Objekt
        // Wird von EF Core per JOIN geladen wenn Include(f => f.User) aufgerufen wird
        public User User { get; set; }

        // Navigationsproperty: die zugehörige Kategorie (kann null sein!)
        // FoodService.GetAll() lädt diese per: .Include(f => f.Category)
        // Quelle: https://learn.microsoft.com/ef/core/querying/related-data/eager
        public Category Category { get; set; }
    }
}