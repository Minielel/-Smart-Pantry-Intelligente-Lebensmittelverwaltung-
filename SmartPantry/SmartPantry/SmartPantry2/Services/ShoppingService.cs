// ------------------------------------------------------------
// Datei: ShoppingService.cs
//
// Beschreibung:
// Diese Datei gehört zur Service-Schicht. Hier werden Aufgaben wie Datenbankzugriffe, Hilfslogik oder das Nachladen von Daten erledigt.
//
// Hinweis fuer die Vorstellung:
// Wenn man diese Datei in der Schule erklaeren moechte, kann man sagen,
// dass sie einen bestimmten Baustein der App uebernimmt und dadurch hilft,
// die Anwendung klar zu strukturieren.
// ------------------------------------------------------------
﻿using Smartpantry.Models;
using SmartPantry2.Data;
using System;
using System.Collections.Generic;
using System.Linq;

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





        public void AddOrMerge(ShoppingItem item)
        {
            using var db = new FoodDbContext();

            var existing = db.ShoppingList
                .FirstOrDefault(s => s.UserId == item.UserId
                                  && !s.IsBought
                                  && s.Name.ToLower() == item.Name.ToLower()
                                  && (s.Unit ?? "") == (item.Unit ?? ""));

            if (existing == null)
            {
                db.ShoppingList.Add(item);
            }
            else
            {
                existing.Amount += item.Amount;
            }

            db.SaveChanges();
        }





        public void UpsertLowStockFromFood(int userId)
        {
            using var db = new FoodDbContext();

            var food = db.FoodItems.Where(f => f.UserId == userId).ToList();
            if (food.Count == 0) return;

            var shopping = db.ShoppingList.Where(s => s.UserId == userId && !s.IsBought).ToList();

            foreach (var f in food)
            {
                var unit = (f.Unit ?? "").Trim();
                var isLow = IsLowStock(f.Amount, unit);
                if (!isLow) continue;

                var exists = shopping.Any(s => ((s.Name ?? "").Trim().ToLower() == (f.Name ?? "").Trim().ToLower())
                                            && (s.Unit ?? "").Trim() == unit);
                if (exists) continue;


                var suggested = SuggestRestockAmount(unit);

                db.ShoppingList.Add(new ShoppingItem
                {
                    UserId = userId,
                    Name = f.Name,
                    Amount = suggested,
                    Unit = unit,
                    IsBought = false
                });
            }

            db.SaveChanges();
        }





        public void MoveToFoodAndRemove(int userId, int shoppingItemId, DateTime expiryDate)
        {
            using var db = new FoodDbContext();
            using var tx = db.Database.BeginTransaction();

            var s = db.ShoppingList.FirstOrDefault(x => x.Id == shoppingItemId && x.UserId == userId);
            if (s == null) return;

            var name = (s.Name ?? "").Trim();
            var unit = (s.Unit ?? "").Trim();


            var targetExpiry = expiryDate.Date;

            var existingFood = db.FoodItems
                .Where(f => f.UserId == userId)
                .AsEnumerable()
                .FirstOrDefault(f =>
                    string.Equals((f.Name ?? "").Trim(), name, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals((f.Unit ?? "").Trim(), unit, StringComparison.OrdinalIgnoreCase) &&
                    f.ExpiryDate.Date == targetExpiry);

            if (existingFood == null)
            {
                db.FoodItems.Add(new FoodItem
                {
                    UserId = userId,
                    Name = name,
                    Amount = s.Amount,
                    Unit = unit,
                    ExpiryDate = targetExpiry,
                    CreatedAt = DateTime.Now,
                    CategoryId = null
                });
            }
            else
            {
                existingFood.Amount += s.Amount;
            }


            db.SaveChanges();

            db.ShoppingList.Remove(s);
            db.SaveChanges();
            tx.Commit();
            FoodService.RaiseFoodChanged();
        }

        private static bool IsLowStock(decimal amount, string unit)
        {
            if (amount <= 0) return true;

            var u = unit.ToLower();
            if (u.Contains("stück") || u.Contains("stk") || u == "st")
                return amount <= 1;

            if (u == "g") return amount <= 100;
            if (u == "ml") return amount <= 100;


            return amount <= 1;
        }

        private static decimal SuggestRestockAmount(string unit)
        {
            var u = (unit ?? "").Trim().ToLower();
            if (u.Contains("stück") || u.Contains("stk") || u == "st") return 3;
            if (u == "g") return 500;
            if (u == "ml") return 1000;
            return 1;
        }

        public void MarkAsBought(int id)
        {
            SetBought(id, true);
        }

        public void SetBought(int id, bool isBought)
        {
            using var db = new FoodDbContext();

            var item = db.ShoppingList.Find(id);
            if (item == null) return;

            item.IsBought = isBought;
            db.SaveChanges();
            FoodService.RaiseFoodChanged();
        }

        public void Delete(int id)
        {
            using var db = new FoodDbContext();
            var item = db.ShoppingList.FirstOrDefault(x => x.Id == id);
            if (item == null) return;

            db.ShoppingList.Remove(item);
            db.SaveChanges();
            FoodService.RaiseFoodChanged();
        }
    }
}
