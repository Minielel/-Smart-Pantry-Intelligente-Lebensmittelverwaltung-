using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
