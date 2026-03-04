using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smartpantry.Models
{
    public class MealPlan
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public int RecipeId { get; set; }

        public DateTime Date { get; set; }

        public string MealType { get; set; }

        public User User { get; set; }

        public Recipe Recipe { get; set; }
    }
}
