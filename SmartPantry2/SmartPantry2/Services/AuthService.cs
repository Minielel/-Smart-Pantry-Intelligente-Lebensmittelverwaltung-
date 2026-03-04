using Microsoft.EntityFrameworkCore;
using Smartpantry.Models;
using SmartPantry2.Data;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace SmartPantry2.Services
{
    public class AuthService
    {
        public User? Login(string username, string password)
        {
            using var db = new FoodDbContext();

            var user = db.Users
                .Include(u => u.Settings)
                .FirstOrDefault(u => u.Username == username);

            if (user == null)
                return null;

            bool valid = BCrypt.Net.BCrypt.Verify(
                password,
                user.PasswordHash
            );

            return valid ? user : null;
        }

        public bool Register(User user, string password)
        {
            using var db = new FoodDbContext();

            if (db.Users.Any(u => u.Username == user.Username))
                return false;

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
            user.CreatedAt = DateTime.Now;

            db.Users.Add(user);
            db.SaveChanges();

            return true;
        }
    }
}
