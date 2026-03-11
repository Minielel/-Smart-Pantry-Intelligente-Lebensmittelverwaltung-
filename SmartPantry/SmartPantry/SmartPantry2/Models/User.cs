// ------------------------------------------------------------
// Datei: User.cs
//
// Beschreibung:
// Diese Datei beschreibt ein Datenmodell. Solche Klassen stellen die Informationen dar, die in der App und in der Datenbank gespeichert werden.
//
// Hinweis fuer die Vorstellung:
// Wenn man diese Datei in der Schule erklaeren moechte, kann man sagen,
// dass sie einen bestimmten Baustein der App uebernimmt und dadurch hilft,
// die Anwendung klar zu strukturieren.
// ------------------------------------------------------------
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smartpantry.Models
{
    public class User
    {
            public int Id { get; set; }

            public string Username { get; set; }

            public string Email { get; set; }

            public string PasswordHash { get; set; }

            public string Role { get; set; }

            public DateTime CreatedAt { get; set; }

            public UserSettings Settings { get; set; }

            public ICollection<FoodItem> FoodItems { get; set; }

            public ICollection<Recipe> Recipes { get; set; }

            public ICollection<MealPlan> MealPlans { get; set; }

            public ICollection<ShoppingItem> ShoppingList { get; set; }
    }

}
