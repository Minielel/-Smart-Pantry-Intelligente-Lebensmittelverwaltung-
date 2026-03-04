using SmartPantry2.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace SmartPantry2.Services
{
    public class DashboardService
    {
        public (int total, int expiring, int recipes) GetStats()
        {
            using var db = new FoodDbContext();

            int total = db.FoodItems.Count();
            int expiring = db.FoodItems
                .Count(f => f.ExpiryDate <= DateTime.Today.AddDays(3));
            int recipes = db.Recipes.Count();

            return (total, expiring, recipes);
        }
    }
}
