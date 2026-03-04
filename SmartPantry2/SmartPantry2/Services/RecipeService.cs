using Microsoft.EntityFrameworkCore;
using Smartpantry.Models;
using SmartPantry2.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace SmartPantry2.Services
{
    public class RecipeService
    {
        public List<Recipe> GetAll()
        {
            using var db = new FoodDbContext();

            return db.Recipes
                .Include(r => r.Ingredients)
                .ToList();
        }

        public void Add(Recipe recipe)
        {
            using var db = new FoodDbContext();
            db.Recipes.Add(recipe);
            db.SaveChanges();
        }

        public void Update(Recipe recipe)
        {
            using var db = new FoodDbContext();
            db.Recipes.Update(recipe);
            db.SaveChanges();
        }

        public void Delete(int id)
        {
            using var db = new FoodDbContext();

            var recipe = db.Recipes.Find(id);
            if (recipe == null) return;

            db.Recipes.Remove(recipe);
            db.SaveChanges();
        }
    }
}
