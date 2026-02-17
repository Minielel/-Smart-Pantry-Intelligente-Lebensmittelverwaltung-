    using Microsoft.EntityFrameworkCore;
    using Smartpantry.Helpers;
    using Smartpantry.Models;
    using System.Configuration;

    namespace Smartpantry.Data
    {
        class FoodDbContext : DbContext
        {
            public DbSet<User> Users { get; set; }
            public DbSet<UserSettings> UserSettings { get; set; }
            public DbSet<Category> Categories { get; set; }
            public DbSet<FoodItem> FoodItems { get; set; }
            public DbSet<Recipe> Recipes { get; set; }
            public DbSet<RecipeIngredient> RecipeIngredients { get; set; }
            public DbSet<MealPlan> MealPlans { get; set; }
            public DbSet<ShoppingItem> ShoppingList { get; set; }

            protected override void OnConfiguring(DbContextOptionsBuilder options)
            {
                // Get connection string from App.config
                string connectionString = DatabaseConfigHelper.GetConnectionString();

                options.UseMySql(
                    connectionString,
                    ServerVersion.AutoDetect(connectionString)
                );
            }
        }
    }
