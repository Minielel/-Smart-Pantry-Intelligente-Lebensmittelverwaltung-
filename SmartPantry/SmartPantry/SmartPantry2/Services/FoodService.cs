// ============================================================
// Datei:   FoodService.cs
// Schicht: Service / Vorratsverwaltung
//
// ZWECK:
//   Alle Datenbankoperationen für Lebensmittel im Vorrat.
//   Enthält intelligente Merge-Logik beim Hinzufügen.
//   Feuert nach jeder Änderung ein globales FoodChanged-Event.
//
// ROTER FADEN:
//   FoodViewModel → FoodService → DB: food_items
//
//   FoodChanged (statisches Event) wird abonniert von:
//   ├─ FoodViewModel.Load()         → Liste neu laden
//   ├─ ShoppingListViewModel.Load() → Low-Stock neu prüfen
//   └─ (indirekt) DashboardViewModel via Timer
//
//   → Eine Vorrats-Änderung aktualisiert automatisch alle Ansichten!
//
// QUELLEN:
//   EF Core – Include() (Eager Loading):
//   https://learn.microsoft.com/ef/core/querying/related-data/eager
//
//   EF Core – AsEnumerable() (clientseitige Auswertung):
//   https://learn.microsoft.com/ef/core/querying/client-eval
//
//   C# Events und Delegates:
//   https://learn.microsoft.com/dotnet/csharp/programming-guide/events/
//
//   LINQ – FirstOrDefault():
//   https://learn.microsoft.com/dotnet/api/system.linq.enumerable.firstordefault
//
//   String.ToLower() für Vergleiche:
//   https://learn.microsoft.com/dotnet/api/system.string.tolower
// ============================================================

using Microsoft.EntityFrameworkCore;
using Smartpantry.Models;
using SmartPantry2.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartPantry2.Services
{
    public class FoodService
    {
        // In diesem Service stehen alle wichtigen Datenbankaktionen fuer Lebensmittel.
        // Dadurch bleibt die eigentliche Benutzeroberflaeche schlanker und besser lesbar.

        // Statisches Event: informiert alle Abonnenten über Vorrats-Änderungen
        // "static" = gehört zur Klasse, nicht zur Instanz
        // → kann von überall mit FoodService.FoodChanged += ... abonniert werden
        // Quelle: https://learn.microsoft.com/dotnet/csharp/programming-guide/events/
        public static event Action? FoodChanged;

        // Reentrancy-Guard: verhindert rekursive Event-Aufrufe
        // Szenario ohne Guard: FoodChanged → ShoppingListViewModel.Load()
        //   → UpsertLowStockFromFood() → FoodService.RaiseFoodChanged()
        //   → wieder FoodChanged → Endlosschleife!
        private static bool _isRaisingFoodChanged;

        // --------------------------------------------------------
        // RaiseFoodChanged
        //
        // FUNKTION:
        //   Feuert das FoodChanged-Event mit Reentrancy-Schutz.
        //   Wird nach JEDER DB-Änderung (Add/Update/Delete) aufgerufen.
        //
        // try/finally: stellt sicher dass _isRaisingFoodChanged auch
        // bei einer Exception zurückgesetzt wird.
        // Quelle: https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/try-finally
        // --------------------------------------------------------
        public static void RaiseFoodChanged()
        {
            // Wenn bereits ein Event läuft → nicht nochmal feuern
            if (_isRaisingFoodChanged) return;

            try
            {
                // Flag setzen: läuft gerade
                _isRaisingFoodChanged = true;
                // Alle Abonnenten benachrichtigen (FoodViewModel.Load() usw.)
                FoodChanged?.Invoke();
            }
            finally
            {
                // Flag zurücksetzen – IMMER, auch bei Exception
                _isRaisingFoodChanged = false;
            }
        }

        // --------------------------------------------------------
        // GetAll
        //
        // FUNKTION:
        //   Lädt alle FoodItems aus der DB inkl. Kategorie-Informationen.
        //
        // RETURN:
        //   List<FoodItem> → sortiert nach Ablaufdatum aufsteigend
        //   (früher ablaufende Items zuerst → Dashboard-Warnung sofort sichtbar)
        //
        // DB-Zugriff:
        //   SELECT fi.*, c.* FROM food_items fi
        //   LEFT JOIN categories c ON fi.category_id = c.id
        //   ORDER BY fi.expiration_date ASC
        //
        // AUFGERUFEN VON: FoodViewModel.Load()
        //   → filtert danach noch nach UserId im ViewModel
        // --------------------------------------------------------
        public List<FoodItem> GetAll()
        {
            using var db = new FoodDbContext();
            return db.FoodItems
                // Include: lädt die verknüpfte Kategorie per JOIN mit
                // → f.Category ist danach nicht mehr null (wenn Kategorie existiert)
                .Include(f => f.Category)
                // Sortierung nach Ablaufdatum: kritischste Items oben
                .OrderBy(f => f.ExpiryDate)
                // Ausführen und Ergebnis als Liste zurückgeben
                // ToList() führt den SQL-Query tatsächlich aus
                .ToList();
        }

        // --------------------------------------------------------
        // Add
        //
        // FUNKTION:
        //   Fügt ein neues FoodItem hinzu ODER addiert die Menge auf
        //   ein vorhandenes Item (Merge-Logik).
        //
        // MERGE-BEDINGUNG:
        //   Gleicher User + gleicher Name (case-insensitive) +
        //   gleiche Einheit + gleiches Ablaufdatum
        //   → existiert schon: Amount += item.Amount (aufaddieren)
        //   → existiert nicht: neuer Datensatz
        //
        // RETURN: void
        //   Nach Abschluss: RaiseFoodChanged() → alle Views aktualisieren
        //
        // WARUM AsEnumerable() statt direkt in SQL?
        //   ToLower() in LINQ wird nicht immer korrekt in MySQL-SQL übersetzt.
        //   AsEnumerable() holt die gefilterten Daten erstmal in den RAM
        //   und führt den String-Vergleich dort durch (clientseitig).
        //   Quelle: https://learn.microsoft.com/ef/core/querying/client-eval
        // --------------------------------------------------------
        public void Add(FoodItem item)
        {
            using var db = new FoodDbContext();

            // Normalisieren für sicheren Vergleich (Großschreibung egal, kein Leerzeichen)
            var normalizedName = (item.Name ?? "").Trim().ToLower();
            var normalizedUnit = (item.Unit ?? "").Trim().ToLower();
            // Nur Datum, keine Uhrzeit → .Date
            var expiryDate = item.ExpiryDate.Date;

            // AsEnumerable(): ab hier wird im RAM verglichen (nicht in SQL)
            // Nötig weil ToLower() nicht sicher in MySQL-SQL übersetzt wird
            var existing = db.FoodItems
                .AsEnumerable()
                .FirstOrDefault(f =>
                    f.UserId == item.UserId &&
                    // Beide Namen kleinschreiben und trimmen für Vergleich
                    ((f.Name ?? "").Trim().ToLower() == normalizedName) &&
                    ((f.Unit ?? "").Trim().ToLower() == normalizedUnit) &&
                    // Nur Datum vergleichen, Uhrzeit ignorieren
                    f.ExpiryDate.Date == expiryDate);

            if (existing == null)
            {
                // Kein Duplikat gefunden → neuen Datensatz anlegen
                db.FoodItems.Add(item);
            }
            else
            {
                // Duplikat gefunden → Menge aufaddieren statt neuen Eintrag
                existing.Amount += item.Amount;
                // "??=" = Null-Coalescing Assignment: nur setzen wenn noch null
                // Quelle: https://learn.microsoft.com/dotnet/csharp/language-reference/operators/null-coalescing-operator
                existing.CategoryId ??= item.CategoryId;
                if (item.CreatedAt != default)
                    existing.CreatedAt = item.CreatedAt;
            }

            // Änderungen in DB schreiben (INSERT oder UPDATE)
            db.SaveChanges();
            // Alle abonnierten ViewModels über die Änderung informieren
            RaiseFoodChanged();
        }

        // --------------------------------------------------------
        // Update
        //
        // FUNKTION: speichert geänderte Werte eines bestehenden FoodItems
        //
        // EF Core Update(): markiert ALLE Properties als "geändert"
        // → UPDATE food_items SET name=?, amount=?, ... WHERE id=?
        // Quelle: https://learn.microsoft.com/ef/core/saving/basic
        // --------------------------------------------------------
        public void Update(FoodItem item)
        {
            using var db = new FoodDbContext();
            // Update() teilt EF Core mit: dieses Objekt hat sich geändert
            db.FoodItems.Update(item);
            db.SaveChanges();
            // Andere Views über Änderung informieren
            RaiseFoodChanged();
        }

        // --------------------------------------------------------
        // Delete
        //
        // FUNKTION: löscht ein FoodItem per Id aus der DB
        //
        // Find(): sucht per Primärschlüssel (id) – schnell da PK-Index
        // Quelle: https://learn.microsoft.com/ef/core/querying/tracking#find-and-findasync
        // --------------------------------------------------------
        public void Delete(int id)
        {
            using var db = new FoodDbContext();

            // Find() nutzt den PK-Index → optimal für ID-Suche
            var item = db.FoodItems.Find(id);
            // Wenn nicht gefunden → nichts tun (defensive Programmierung)
            if (item == null) return;

            // Remove(): markiert Entity für Löschung
            // SaveChanges(): führt DELETE-SQL aus
            db.FoodItems.Remove(item);
            db.SaveChanges();
            RaiseFoodChanged();
        }

        // --------------------------------------------------------
        // GetExpiringSoon
        //
        // FUNKTION:
        //   Gibt alle FoodItems zurück die innerhalb der nächsten
        //   X Tage ablaufen (Standard: 3 Tage).
        //
        // RETURN: List<FoodItem> mit Ablaufdatum <= heute + X Tage
        //
        // AUFGERUFEN VON: (zurzeit nicht direkt, DashboardService hat eigene Logik)
        //   Kann für spätere Features genutzt werden (z.B. E-Mail-Benachrichtigung)
        // --------------------------------------------------------
        public List<FoodItem> GetExpiringSoon(int days = 3)
        {
            using var db = new FoodDbContext();

            // Grenzwert: heute + X Tage
            var limit = DateTime.Today.AddDays(days);

            return db.FoodItems
                // Alle Items deren Ablaufdatum vor oder am Grenzwert liegt
                .Where(f => f.ExpiryDate <= limit)
                .ToList();
        }
    }
}