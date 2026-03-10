// ============================================================
// Datei:   ShoppingItem.cs
// Schicht: Model / Datenmodell
// Datenbanktabelle: shopping_list
//
// ZWECK:
//   Ein Eintrag auf der Einkaufsliste des Users.
//   Kann automatisch durch Low-Stock-Erkennung entstehen
//   oder manuell vom User hinzugefügt werden.
//
// ROTER FADEN:
//   users ──→ shopping_list (1:n, über user_id)
//
//   LEBENSWEG eines ShoppingItems:
//   1. Entstehung: ShoppingService.UpsertLowStockFromFood() (automatisch)
//      ODER ShoppingListViewModel.Add() (manuell durch User)
//   2. Anzeige: ShoppingListView.xaml zeigt Items als Kacheln
//   3. Transfer: ShoppingService.MoveToFoodAndRemove()
//      → Item wird zu FoodItem → aus shopping_list gelöscht
//
//   WICHTIGER NAME-UNTERSCHIED:
//   C# Property "IsBought" ↔ DB-Spalte "checked"
//   Mapping in FoodDbContext:
//     e.Property(p => p.IsBought).HasColumnName("checked");
//   (Warum unterschiedlich? "checked" ist ein SQL-Keyword und
//    "IsBought" ist aussagekräftiger im C#-Code)
//
// USER USECASE:
//   User öffnet Einkaufsliste
//   → Items mit niedrigem Bestand erscheinen automatisch
//   → User fügt manuell weitere Items hinzu
//   → User geht einkaufen, kommt zurück
//   → User klickt "Zu Food" Checkbox → Item → Vorrat
//
// QUELLEN:
//   EF Core – Column Mapping (HasColumnName):
//   https://learn.microsoft.com/ef/core/modeling/entity-properties#column-names
//
//   string.Empty vs "": Best Practice in C#:
//   https://learn.microsoft.com/dotnet/api/system.string.empty
// ============================================================

namespace Smartpantry.Models
{
    public class ShoppingItem
    {
        // Primärschlüssel → DB-Spalte "id"
        public int Id { get; set; }

        // Fremdschlüssel zu users.id → DB-Spalte "user_id"
        public int UserId { get; set; }

        // Name des einzukaufenden Artikels (z.B. "Milch", "Brot")
        // "= string.Empty" → nie null, verhindert NullReferenceException
        // DB-Spalte: "name"
        public string Name { get; set; } = string.Empty;

        // Einzukaufende Menge (z.B. 500 für "500g")
        // decimal → DB: DECIMAL(10,2)
        // DB-Spalte: "amount"
        public decimal Amount { get; set; }

        // Einheit (z.B. "g", "ml", "Stück")
        // "= string.Empty" → nie null
        // DB-Spalte: "unit"
        public string Unit { get; set; } = string.Empty;

        // Ob das Item bereits eingekauft/übernommen wurde
        // ACHTUNG: C# "IsBought" ↔ DB-Spalte "checked" (Mapping in FoodDbContext!)
        // true = wurde zu Food übernommen (und sollte bald gelöscht werden)
        // false = noch nicht eingekauft
        // DB-Spalte: "checked"
        public bool IsBought { get; set; }

        // Navigationsproperty: der zugehörige User
        // "?" = nullable: wird nicht in jedem Query geladen
        public User? User { get; set; }
    }
}