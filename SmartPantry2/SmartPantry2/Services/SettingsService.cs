using Smartpantry.Models;
using SmartPantry2.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace SmartPantry2.Services
{
    public class SettingsService
    {
        public UserSettings? Get(int userId)
        {
            using var db = new FoodDbContext();
            return db.UserSettings.FirstOrDefault(s => s.UserId == userId);
        }

        public void Update(UserSettings settings)
        {
            using var db = new FoodDbContext();
            db.UserSettings.Update(settings);
            db.SaveChanges();
        }
    }
}
