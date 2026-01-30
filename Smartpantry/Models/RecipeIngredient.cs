using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smartpantry.Models
{
    public class RecipeIngredient
    {
        public int Id { get; set; }

        public int RecipeId { get; set; }

        public string FoodItemName { get; set; }

        public decimal Amount { get; set; }

        public string Unit { get; set; }

        public Recipe Recipe { get; set; }
    }
}
