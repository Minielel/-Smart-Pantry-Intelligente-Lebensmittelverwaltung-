// ============================================================
// Datei:   DashboardService.cs
// Schicht: Service / Statistiken
//
// ZWECK:
//   Berechnet alle Dashboard-Kennzahlen in einem einzigen
//   Datenbankaufruf (4 COUNT-Abfragen in einer Methode).
//
// ROTER FADEN:
//   DashboardViewModel.Refresh()
//     → DashboardService.GetStats(userId)
//       → DB: food_items + recipes (nur COUNT, keine Daten laden)
//     → zurück: (total, expiringSoon, expired, recipes) als Tupel
//     → DashboardViewModel setzt TotalFoodItems, ExpiringSoon usw.
//     → DashboardView.xaml zeigt Zahlen in Kacheln
//     → ExpiryAlertLevel steuert Warnfarbe der "Läuft bald ab"-Kachel
//
// USER USECASE:
//   App-Start → sofortige Übersicht über den Vorratsstand.
//   Alle 30 Sekunden automatische Aktualisierung.
//   Manuelle Aktualisierung per "Aktualisieren"-Button.
//
// QUELLEN:
//   LINQ – AsQueryable() (für dynamische Queries):
//   https://learn.microsoft.com/dotnet/api/system.linq.queryable.asqueryable
//
//   LINQ – Count() mit Prädikat:
//   https://learn.microsoft.com/dotnet/api/system.linq.queryable.count
//
//   C# Tuple Return Types:
//   https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/value-tuples
//
//   DateTime.Today (nur Datum, keine Uhrzeit):
//   https://learn.microsoft.com/dotnet/api/system.datetime.today
// ============================================================

using SmartPantry2.Data;
using System;
using System.Linq;

namespace SmartPantry2.Services
{
    public class DashboardService
    {
        // --------------------------------------------------------
        // GetStats
        //
        // FUNKTION:
        //   Berechnet 4 Kennzahlen in einem Datenbankaufruf.
        //   Optional filterbar nach einem bestimmten User.
        //
        // PARAMETER:
        //   userId (optional) → wenn angegeben: nur Daten dieses Users
        //                       wenn null: alle Daten (Admin-Ansicht)
        //
        // RETURN: C#-Tupel mit 4 Werten (named tuple):
        //   total        → Gesamtanzahl FoodItems des Users
        //   expiringSoon → Items die HEUTE oder in den nächsten 3 Tagen ablaufen
        //   expired      → Items die BEREITS abgelaufen sind (ExpiryDate < heute)
        //   recipes      → Anzahl der Rezepte des Users
        //
        // DB-Zugriff (4 COUNT-Queries):
        //   SELECT COUNT(*) FROM food_items WHERE user_id = ?
        //   SELECT COUNT(*) FROM food_items WHERE user_id = ? AND expiration_date < TODAY
        //   SELECT COUNT(*) FROM food_items WHERE user_id = ? AND expiration_date BETWEEN TODAY AND TODAY+3
        //   SELECT COUNT(*) FROM recipes WHERE user_id = ?
        //
        // AUFGERUFEN VON: DashboardViewModel.Refresh()
        // --------------------------------------------------------
        public (int total, int expiringSoon, int expired, int recipes) GetStats(int? userId = null)
        {
            using var db = new FoodDbContext();

            // AsQueryable() → Query wird erst beim Aufruf von Count() ausgeführt
            // → ermöglicht dynamisches Hinzufügen von Where-Bedingungen
            // Quelle: https://learn.microsoft.com/dotnet/api/system.linq.queryable.asqueryable
            var food = db.FoodItems.AsQueryable();
            var recipesQ = db.Recipes.AsQueryable();

            // Wenn userId angegeben: auf diesen User einschränken
            // HasValue prüft ob nullable int einen Wert hat
            if (userId.HasValue)
            {
                food = food.Where(f => f.UserId == userId.Value);
                recipesQ = recipesQ.Where(r => r.UserId == userId.Value);
            }

            // COUNT(*) – kein Datenladen, nur Zählen (performant)
            int total = food.Count();

            // Items die bereits abgelaufen sind (gestern oder früher)
            // DateTime.Today = aktuelles Datum ohne Uhrzeit
            int expired = food.Count(f => f.ExpiryDate < DateTime.Today);

            // Items die heute oder in den nächsten 3 Tagen ablaufen
            // >= Today: heute ablaufende Items zählen auch als "bald ablaufend"
            // <= Today.AddDays(3): bis einschließlich übermorgen
            int expiringSoon = food.Count(f => f.ExpiryDate >= DateTime.Today && f.ExpiryDate <= DateTime.Today.AddDays(3));

            // Anzahl der Rezepte des Users
            int recipes = recipesQ.Count();

            // C# Value Tuple: gibt 4 Werte als ein Objekt zurück
            // Quelle: https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/value-tuples
            return (total, expiringSoon, expired, recipes);
        }
    }
}