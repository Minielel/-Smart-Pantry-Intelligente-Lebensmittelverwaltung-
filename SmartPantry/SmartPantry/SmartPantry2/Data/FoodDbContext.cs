// ------------------------------------------------------------
// Datei: FoodDbContext.cs
//
// Beschreibung:
// Diese Datei verbindet die App mit der Datenbank. Sie legt fest, welche Tabellen es gibt und wie Klassen auf Datenbankspalten abgebildet werden.
//
// Hinweis fuer die Vorstellung:
// Wenn man diese Datei in der Schule erklaeren moechte, kann man sagen,
// dass sie einen bestimmten Baustein der App uebernimmt und dadurch hilft,
// die Anwendung klar zu strukturieren.
// ------------------------------------------------------------
﻿using Microsoft.EntityFrameworkCore;
using Smartpantry.Helpers;
using Smartpantry.Models;

namespace SmartPantry2.Data
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

            string connectionString = DatabaseConfigHelper.GetConnectionString();

            options.UseMySql(
                connectionString,
                ServerVersion.AutoDetect(connectionString)
            );
        }







        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {




            modelBuilder.Entity<User>(e =>
            {
                e.ToTable("users");
                e.Property(p => p.Id).HasColumnName("id");
                e.Property(p => p.Username).HasColumnName("username");
                e.Property(p => p.Email).HasColumnName("email");
                e.Property(p => p.PasswordHash).HasColumnName("password_hash");
                e.Property(p => p.Role).HasColumnName("role");
                e.Property(p => p.CreatedAt).HasColumnName("created_at");
            });

            modelBuilder.Entity<UserSettings>(e =>
            {
                e.ToTable("user_settings");
                e.Property(p => p.Id).HasColumnName("id");
                e.Property(p => p.UserId).HasColumnName("user_id");
                e.Property(p => p.Theme).HasColumnName("theme");
                e.Property(p => p.Language).HasColumnName("language");
            });

            modelBuilder.Entity<Category>(e =>
            {
                e.ToTable("categories");
                e.Property(p => p.Id).HasColumnName("id");
                e.Property(p => p.Name).HasColumnName("name");
            });


            modelBuilder.Entity<FoodItem>(e =>
            {
                e.ToTable("food_items");
                e.Property(p => p.Id).HasColumnName("id");
                e.Property(p => p.UserId).HasColumnName("user_id");
                e.Property(p => p.Name).HasColumnName("name");
                e.Property(p => p.Amount).HasColumnName("amount");
                e.Property(p => p.Unit).HasColumnName("unit");

                e.Property(p => p.ExpiryDate).HasColumnName("expiration_date");
                e.Property(p => p.CategoryId).HasColumnName("category_id");
                e.Property(p => p.CreatedAt).HasColumnName("created_at");
            });

            modelBuilder.Entity<Recipe>(e =>
            {
                e.ToTable("recipes");
                e.Property(p => p.Id).HasColumnName("id");
                e.Property(p => p.UserId).HasColumnName("user_id");
                e.Property(p => p.Name).HasColumnName("name");
                e.Property(p => p.Description).HasColumnName("description");
                e.Property(p => p.Instructions).HasColumnName("instructions");
                e.Property(p => p.CreatedAt).HasColumnName("created_at");
            });


            modelBuilder.Entity<RecipeIngredient>(e =>
            {
                e.ToTable("recipe_ingredients");
                e.Property(p => p.Id).HasColumnName("id");
                e.Property(p => p.RecipeId).HasColumnName("recipe_id");

                e.Property(p => p.FoodItem).HasColumnName("food_item_name");
                e.Property(p => p.Amount).HasColumnName("amount");
                e.Property(p => p.Unit).HasColumnName("unit");
            });

            modelBuilder.Entity<MealPlan>(e =>
            {
                e.ToTable("meal_plan");
                e.Property(p => p.Id).HasColumnName("id");
                e.Property(p => p.UserId).HasColumnName("user_id");
                e.Property(p => p.RecipeId).HasColumnName("recipe_id");
                e.Property(p => p.Date).HasColumnName("date");
                e.Property(p => p.MealType).HasColumnName("meal_type");
            });


            modelBuilder.Entity<ShoppingItem>(e =>
            {
                e.ToTable("shopping_list");
                e.Property(p => p.Id).HasColumnName("id");
                e.Property(p => p.UserId).HasColumnName("user_id");
                e.Property(p => p.Name).HasColumnName("name");
                e.Property(p => p.Amount).HasColumnName("amount");
                e.Property(p => p.Unit).HasColumnName("unit");

                e.Property(p => p.IsBought).HasColumnName("checked");
            });
        }
    }
}
