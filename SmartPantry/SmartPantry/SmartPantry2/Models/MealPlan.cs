// ============================================================
// Datei:   MealPlan.cs
// Schicht: Model / Datenmodell
// Datenbanktabelle: meal_plan
//
// ZWECK:
//   Repräsentiert einen einzelnen Wochenplan-Eintrag.
//   Verknüpft: User + Rezept + Datum + Mahlzeit (Morgens/Mittags/Abends).
//
// ROTER FADEN:
//   users   ──→ meal_plan (1:n, über user_id)
//   recipes ──→ meal_plan (1:n, über recipe_id)
//   → meal_plan ist eine Verbindungstabelle zwischen users und recipes
//     mit zusätzlichen Feldern (date, meal_type)
//
//   MealPlanService.GetWeekPlan() lädt Pläne mit Include(m => m.Recipe)
//   → Recipe.Name ist dann verfügbar für die Anzeige in MealPlanView.xaml
//
//   DB MealType-Werte: "breakfast" | "lunch" | "dinner" (ENUM in SQL)
//   MealPlanView.xaml zeigt per DataTrigger: "Morgens" | "Mittags" | "Abends"
//
// USER USECASE:
//   Admin öffnet "Plan"
//   → MealPlanService.GetWeekPlan() → alle Planeinträge laden
//   → Admin klickt "Rezept auswählen" → wechselt zu Rezepte-Auswahl
//   → Admin wählt Datum + Mahlzeit → "Hinzufügen"
//   → neuer meal_plan-Datensatz in DB
//
// QUELLEN:
//   EF Core – Many-to-Many über explizite Verbindungsentität:
//   https://learn.microsoft.com/ef/core/modeling/relationships/many-to-many
//
//   EF Core – Eager Loading mit Include():
//   https://learn.microsoft.com/ef/core/querying/related-data/eager
// ============================================================

namespace Smartpantry.Models
{
    public class MealPlan
    {
        // Primärschlüssel → DB-Spalte "id"
        public int Id { get; set; }

        // Fremdschlüssel zu users.id → DB-Spalte "user_id"
        public int UserId { get; set; }

        // Fremdschlüssel zu recipes.id → DB-Spalte "recipe_id"
        public int RecipeId { get; set; }

        // Geplantes Datum (nur Datum, keine Uhrzeit)
        // DB-Spalte: "date" (DATE-Typ in SQL)
        public DateTime Date { get; set; }

        // Mahlzeittyp: "breakfast" | "lunch" | "dinner"
        // DB: ENUM('breakfast','lunch','dinner')
        // MealPlanView.xaml übersetzt per DataTrigger auf Deutsch
        // DB-Spalte: "meal_type"
        public string MealType { get; set; }

        // Navigationsproperty: der zugehörige User
        public User User { get; set; }

        // Navigationsproperty: das zugehörige Rezept
        // WICHTIG: wird von MealPlanService per Include(m => m.Recipe) geladen
        // → Recipe.Name wird in MealPlanView.xaml angezeigt:
        //   <TextBlock Text="{Binding Recipe.Name}"/>
        // Quelle: https://learn.microsoft.com/ef/core/querying/related-data/eager
        public Recipe Recipe { get; set; }
    }
}