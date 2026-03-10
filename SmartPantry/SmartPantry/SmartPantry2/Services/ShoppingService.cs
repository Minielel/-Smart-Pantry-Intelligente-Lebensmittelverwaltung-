// ============================================================
// Datei:   ShoppingService.cs
// Schicht: Service / Einkaufsliste
//
// ZWECK:
//   Alle Datenbankoperationen für die Einkaufsliste.
//   Enthält Low-Stock-Erkennung und den Transfer Shopping → Food.
//
// ROTER FADEN:
//   ShoppingListViewModel → ShoppingService → DB: shopping_list + food_items
//
//   BESONDERE METHODEN:
//   1. AddOrMerge()             → Duplikat-Schutz beim Hinzufügen
//   2. UpsertLowStockFromFood() → automatische Vorschlags-Generierung
//   3. MoveToFoodAndRemove()    → Kernfunktion "Zu Food" → Transfer + Löschung
//
// QUELLEN:
//   EF Core – Database Transactions:
//   https://learn.microsoft.com/ef/core/saving/transactions
//
//   EF Core – AsEnumerable() (clientseitige Auswertung):
//   https://learn.microsoft.com/ef/core/querying/client-eval
//
//   StringComparison.OrdinalIgnoreCase:
//   https://learn.microsoft.com/dotnet/api/system.stringcomparison
//
//   LINQ – Any() (Existenzprüfung):
//   https://learn.microsoft.com/dotnet/api/system.linq.enumerable.any
// ============================================================

using Smartpantry.Models;
using SmartPantry2.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartPantry2.Services
{
    public class ShoppingService
    {
        // --------------------------------------------------------
        // GetAll
        //
        // RETURN: alle ShoppingItems eines Users aus der DB
        //   (sowohl gekaufte als auch nicht-gekaufte)
        //
        // DB: SELECT * FROM shopping_list WHERE user_id = ?
        // AUFGERUFEN VON: ShoppingListViewModel.Load()
        // --------------------------------------------------------
        public List<ShoppingItem> GetAll(int userId)
        {
            using var db = new FoodDbContext();

            return db.ShoppingList
                // Nur Items des angegebenen Users
                .Where(s => s.UserId == userId)
                .ToList();
        }

        // --------------------------------------------------------
        // Add (einfache Version ohne Merge)
        //
        // FUNKTION: Fügt ShoppingItem direkt in die DB ein.
        //   (Ohne Duplikat-Prüfung – für interne Verwendung)
        // DB: INSERT INTO shopping_list (...)
        // --------------------------------------------------------
        public void Add(ShoppingItem item)
        {
            using var db = new FoodDbContext();
            db.ShoppingList.Add(item);
            db.SaveChanges();
        }

        // --------------------------------------------------------
        // AddOrMerge
        //
        // FUNKTION:
        //   Fügt Item hinzu ODER addiert Menge wenn Duplikat existiert.
        //   Duplikat = gleicher Name + gleiche Einheit + noch nicht gekauft
        //   (IsBought = false).
        //
        // WARUM?
        //   Verhindert dass "Milch 500g" doppelt auf der Liste erscheint.
        //   Stattdessen: Menge wird auf 1000g erhöht.
        //
        // DB: INSERT INTO shopping_list (...) ODER UPDATE ... SET amount=?
        //
        // AUFGERUFEN VON: ShoppingListViewModel.Add() (manuelles Hinzufügen)
        // --------------------------------------------------------
        public void AddOrMerge(ShoppingItem item)
        {
            using var db = new FoodDbContext();

            // Suche nach nicht-gekauftem Item mit gleichem Namen und Einheit
            var existing = db.ShoppingList
                .FirstOrDefault(s => s.UserId == item.UserId
                                  // Nur nicht-gekaufte Items prüfen
                                  && !s.IsBought
                                  // Groß-/Kleinschreibung ignorieren
                                  && s.Name.ToLower() == item.Name.ToLower()
                                  // Einheit muss übereinstimmen (null-safe mit "")
                                  && (s.Unit ?? "") == (item.Unit ?? ""));

            if (existing == null)
            {
                // Kein Duplikat: neues Item hinzufügen
                db.ShoppingList.Add(item);
            }
            else
            {
                // Duplikat gefunden: Menge aufaddieren
                existing.Amount += item.Amount;
            }

            db.SaveChanges();
        }

        // --------------------------------------------------------
        // UpsertLowStockFromFood
        //
        // FUNKTION:
        //   Analysiert den Vorrat des Users auf niedrige Bestände.
        //   Für jedes Item mit niedrigem Bestand das noch NICHT auf
        //   der Einkaufsliste steht: automatisch hinzufügen.
        //
        // "Upsert" = Update + Insert: fügt ein wenn nicht vorhanden
        //
        // LOW-STOCK-SCHWELLEN (IsLowStock):
        //   Stück/Stk/st → ≤ 1     → empfohlener Nachkauf: 3 Stück
        //   Gramm (g)    → ≤ 100g  → empfohlener Nachkauf: 500g
        //   Milliliter   → ≤ 100ml → empfohlener Nachkauf: 1000ml
        //   Alles andere → ≤ 1
        //
        // RETURN: void → Ändert DB direkt, ViewModel liest danach neu
        //
        // AUFGERUFEN VON: ShoppingListViewModel.Load()
        //   → bei jedem Öffnen der Einkaufsliste automatisch
        // --------------------------------------------------------
        public void UpsertLowStockFromFood(int userId)
        {
            using var db = new FoodDbContext();

            // Alle FoodItems des Users laden
            var food = db.FoodItems.Where(f => f.UserId == userId).ToList();
            // Wenn keine Items vorhanden → nichts zu prüfen
            if (food.Count == 0) return;

            // Alle noch nicht-gekauften Shopping-Items laden (für Duplikat-Prüfung)
            var shopping = db.ShoppingList.Where(s => s.UserId == userId && !s.IsBought).ToList();

            foreach (var f in food)
            {
                var unit = (f.Unit ?? "").Trim();

                // Ist der Bestand niedrig? (Hilfsmethode prüft je nach Einheit)
                var isLow = IsLowStock(f.Amount, unit);
                // Kein niedriger Bestand → überspringen
                if (!isLow) continue;

                // Ist dieses Item schon auf der Einkaufsliste? (Duplikat vermeiden)
                var exists = shopping.Any(s =>
                    ((s.Name ?? "").Trim().ToLower() == (f.Name ?? "").Trim().ToLower())
                    && (s.Unit ?? "").Trim() == unit);
                // Schon drauf → nicht nochmal hinzufügen
                if (exists) continue;

                // Empfohlene Nachkauf-Menge je nach Einheit bestimmen
                var suggested = SuggestRestockAmount(unit);

                // Neues Shopping-Item automatisch anlegen
                db.ShoppingList.Add(new ShoppingItem
                {
                    UserId = userId,
                    Name = f.Name,
                    Amount = suggested,
                    Unit = unit,
                    IsBought = false  // noch nicht gekauft
                });
            }

            db.SaveChanges();
        }

        // --------------------------------------------------------
        // MoveToFoodAndRemove
        //
        // FUNKTION:
        //   Kernfunktion des "Zu Food" Buttons in ShoppingListView.
        //   Überträgt ein Shopping-Item mit Ablaufdatum in den Vorrat
        //   und löscht es aus der Einkaufsliste.
        //   Läuft in einer Datenbank-TRANSAKTION: alles oder nichts.
        //
        // PARAMETER:
        //   userId         → Sicherheitsprüfung: nur eigene Items
        //   shoppingItemId → welches Item übernommen werden soll
        //   expiryDate     → Ablaufdatum für das neue FoodItem
        //                    (User wählt es per DatePicker in ShoppingListView)
        //
        // RETURN: void
        //   Nach Abschluss: FoodService.RaiseFoodChanged() → alle Views aktualisieren
        //
        // TRANSAKTION:
        //   BeginTransaction() → SaveChanges (Insert Food) → Remove Shopping
        //   → SaveChanges → Commit
        //   Bei Fehler: automatisches Rollback (kein halbfertiger Zustand)
        //
        // USER USECASE:
        //   1. User hat Milch eingekauft
        //   2. User setzt Ablaufdatum in der rechten Seitenleiste
        //   3. User klickt "Zu Food" Checkbox bei der Milch-Kachel
        //   4. Milch erscheint in food_items mit dem gewählten Ablaufdatum
        //   5. Milch verschwindet von der Einkaufsliste
        // --------------------------------------------------------
        public void MoveToFoodAndRemove(int userId, int shoppingItemId, DateTime expiryDate)
        {
            using var db = new FoodDbContext();
            // Transaktion starten: beide DB-Operationen als Einheit
            // Quelle: https://learn.microsoft.com/ef/core/saving/transactions
            using var tx = db.Database.BeginTransaction();

            // Shopping-Item laden (mit User-Prüfung: nur eigene Items!)
            var s = db.ShoppingList.FirstOrDefault(x => x.Id == shoppingItemId && x.UserId == userId);
            // Item nicht gefunden oder gehört anderem User → abbrechen
            if (s == null) return;

            // Daten aus Shopping-Item extrahieren und normalisieren
            var name = (s.Name ?? "").Trim();
            var unit = (s.Unit ?? "").Trim();

            // Nur das Datum, keine Uhrzeit
            var targetExpiry = expiryDate.Date;

            // Prüfen ob gleiches Item mit gleichem Ablaufdatum schon im Vorrat
            var existingFood = db.FoodItems
                .Where(f => f.UserId == userId)
                // AsEnumerable(): clientseitiger Vergleich nötig für OrdinalIgnoreCase
                .AsEnumerable()
                .FirstOrDefault(f =>
                    // OrdinalIgnoreCase: "Milch" == "milch" == "MILCH"
                    string.Equals((f.Name ?? "").Trim(), name, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals((f.Unit ?? "").Trim(), unit, StringComparison.OrdinalIgnoreCase) &&
                    // Gleicher Tag? (.Date entfernt die Uhrzeit)
                    f.ExpiryDate.Date == targetExpiry);

            if (existingFood == null)
            {
                // Noch nicht im Vorrat → neues FoodItem anlegen
                db.FoodItems.Add(new FoodItem
                {
                    UserId = userId,
                    Name = name,
                    Amount = s.Amount,
                    Unit = unit,
                    ExpiryDate = targetExpiry,
                    CreatedAt = DateTime.Now,
                    CategoryId = null  // keine Kategorie bei automatischem Transfer
                });
            }
            else
            {
                // Schon im Vorrat mit gleichem Ablaufdatum → Menge aufaddieren
                existingFood.Amount += s.Amount;
            }

            // 1. Änderung (neues FoodItem oder erhöhte Menge) speichern
            db.SaveChanges();

            // 2. Shopping-Item aus der Liste entfernen
            db.ShoppingList.Remove(s);

            // 3. Löschung speichern
            db.SaveChanges();

            // Transaktion abschließen: beide Operationen als Einheit bestätigen
            tx.Commit();

            // Alle Views über Vorrats-Änderung informieren
            FoodService.RaiseFoodChanged();
        }

        // --------------------------------------------------------
        // IsLowStock (privat)
        //
        // FUNKTION: prüft ob ein Bestand als "niedrig" gilt
        //
        // RETURN: true = niedrig → sollte auf Einkaufsliste
        //
        // Schwellenwerte je nach Einheit:
        //   Stück (stück/stk/st) → ≤ 1 Stück
        //   Gramm (g)            → ≤ 100g
        //   Milliliter (ml)      → ≤ 100ml
        //   Alles andere         → ≤ 1
        // --------------------------------------------------------
        private static bool IsLowStock(decimal amount, string unit)
        {
            // Menge 0 oder negativ → immer "niedrig"
            if (amount <= 0) return true;

            // Einheit kleinschreiben für Vergleich
            var u = unit.ToLower();

            // Stück-Einheiten: niedrig wenn ≤ 1
            if (u.Contains("stück") || u.Contains("stk") || u == "st")
                return amount <= 1;

            // Gramm: niedrig wenn ≤ 100g
            if (u == "g") return amount <= 100;

            // Milliliter: niedrig wenn ≤ 100ml
            if (u == "ml") return amount <= 100;

            // Fallback für unbekannte Einheiten: ≤ 1
            return amount <= 1;
        }

        // --------------------------------------------------------
        // SuggestRestockAmount (privat)
        //
        // FUNKTION: schlägt eine sinnvolle Nachkauf-Menge vor
        //
        // RETURN: decimal → empfohlene Menge für das Shopping-Item
        // --------------------------------------------------------
        private static decimal SuggestRestockAmount(string unit)
        {
            var u = (unit ?? "").Trim().ToLower();
            // Stück: 3 nachkaufen (sinnvolle Vorratsgröße)
            if (u.Contains("stück") || u.Contains("stk") || u == "st") return 3;
            // Gramm: 500g (eine Packung)
            if (u == "g") return 500;
            // Milliliter: 1 Liter
            if (u == "ml") return 1000;
            // Fallback
            return 1;
        }

        // --------------------------------------------------------
        // MarkAsBought / SetBought
        //
        // FUNKTION: setzt IsBought auf true oder false
        //   MarkAsBought ist ein Shortcut für SetBought(id, true)
        //
        // DB: UPDATE shopping_list SET checked=? WHERE id=?
        // --------------------------------------------------------
        public void MarkAsBought(int id)
        {
            SetBought(id, true);
        }

        public void SetBought(int id, bool isBought)
        {
            using var db = new FoodDbContext();

            // Find(): sucht per PK-Index (optimal)
            var item = db.ShoppingList.Find(id);
            if (item == null) return;

            // IsBought setzen → EF Core erkennt die Änderung
            item.IsBought = isBought;
            db.SaveChanges();
            // Auch FoodViewModel und Dashboard über Änderung informieren
            FoodService.RaiseFoodChanged();
        }

        // --------------------------------------------------------
        // Delete
        //
        // FUNKTION: löscht ein Shopping-Item dauerhaft
        // DB: DELETE FROM shopping_list WHERE id=?
        // AUFGERUFEN VON: ShoppingListViewModel.DeleteItem()
        // --------------------------------------------------------
        public void Delete(int id)
        {
            using var db = new FoodDbContext();
            var item = db.ShoppingList.FirstOrDefault(x => x.Id == id);
            if (item == null) return;

            db.ShoppingList.Remove(item);
            db.SaveChanges();
            FoodService.RaiseFoodChanged();
        }
    }
}