using Smartpantry.Models;
using SmartPantry2.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace SmartPantry2.Services
{
    public class ShoppingService
    {
        public List<ShoppingItem> GetAll(int userId)
        {
            using var db = new FoodDbContext();

            return db.ShoppingList
                .Where(s => s.UserId == userId)
                .ToList();
        }

        public void Add(ShoppingItem item)
        {
            using var db = new FoodDbContext();
            db.ShoppingList.Add(item);
            db.SaveChanges();
        }

        public void MarkAsBought(int id)
        {
            using var db = new FoodDbContext();

            var item = db.ShoppingList.Find(id);
            if (item == null) return;

            item.IsBought = true;
            db.SaveChanges();
        }
    }
}
