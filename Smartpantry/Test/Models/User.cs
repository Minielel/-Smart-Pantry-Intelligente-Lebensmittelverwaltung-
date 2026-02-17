using System;
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

