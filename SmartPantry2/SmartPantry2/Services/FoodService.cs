using Microsoft.EntityFrameworkCore;
using Smartpantry.Models;
using SmartPantry2.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace SmartPantry2.Services
{
    public class FoodService
    {
        public List<FoodItem> GetAll()
        {
            using var db = new FoodDbContext();
            return db.FoodItems
                .Include(f => f.Category)
                .OrderBy(f => f.ExpiryDate)
                .ToList();
        }

        public void Add(FoodItem item)
        {
            using var db = new FoodDbContext();
            db.FoodItems.Add(item);
            db.SaveChanges();
        }

        public void Update(FoodItem item)
        {
            using var db = new FoodDbContext();
            db.FoodItems.Update(item);
            db.SaveChanges();
        }

        public void Delete(int id)
        {
            using var db = new FoodDbContext();

            var item = db.FoodItems.Find(id);
            if (item == null) return;

            db.FoodItems.Remove(item);
            db.SaveChanges();
        }

        public List<FoodItem> GetExpiringSoon(int days = 3)
        {
            using var db = new FoodDbContext();

            var limit = DateTime.Today.AddDays(days);

            return db.FoodItems
                .Where(f => f.ExpiryDate <= limit)
                .ToList();
        }
    }
}