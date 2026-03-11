// ------------------------------------------------------------
// Datei: Recipe.cs
//
// Beschreibung:
// Diese Datei beschreibt ein Datenmodell. Solche Klassen stellen die Informationen dar, die in der App und in der Datenbank gespeichert werden.
//
// Hinweis fuer die Vorstellung:
// Wenn man diese Datei in der Schule erklaeren moechte, kann man sagen,
// dass sie einen bestimmten Baustein der App uebernimmt und dadurch hilft,
// die Anwendung klar zu strukturieren.
// ------------------------------------------------------------
using System.ComponentModel.DataAnnotations.Schema;
using System;
using System.Collections.Generic;
namespace Smartpantry.Models
{
    public class Recipe
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public string Instructions { get; set; }

        public DateTime CreatedAt { get; set; }

        public User User { get; set; }

        public ICollection<RecipeIngredient> Ingredients { get; set; }

        public ICollection<MealPlan> MealPlans { get; set; }
        [NotMapped]
        public string? ImagePath { get; set; }
}
}
