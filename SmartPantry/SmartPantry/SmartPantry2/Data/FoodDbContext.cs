// ============================================================
// Datei:   FoodDbContext.cs
// Schicht: Data / Datenbankzugriff
//
// ZWECK:
//   Entity Framework Core DbContext – die zentrale Brücke
//   zwischen C#-Objekten (Models) und der MySQL-Datenbank.
//   Definiert alle Tabellen als DbSet<T> und mappt
//   C#-Propertynamen (PascalCase) auf SQL-Spaltennamen (snake_case).
//
// ROTER FADEN:
//   Jeder Service erstellt eine kurze Datenbankverbindung:
//     using var db = new FoodDbContext();
//   → OnConfiguring() wird intern von EF Core aufgerufen
//   → Verbindungsstring aus App.config via DatabaseConfigHelper
//   → LINQ-Abfragen auf DbSets werden in SQL übersetzt:
//     db.FoodItems.Where(f => f.UserId == x).ToList()
//     → SELECT * FROM food_items WHERE user_id = x
//
// KRITISCHE COLUMN-MAPPINGS (C# ↔ DB):
//   FoodItem.ExpiryDate      ↔ "expiration_date"  (UNTERSCHIEDLICH!)
//   RecipeIngredient.FoodItem ↔ "food_item_name"  (UNTERSCHIEDLICH!)
//   ShoppingItem.IsBought     ↔ "checked"          (UNTERSCHIEDLICH!)
//   Alle anderen: C# → lowercase, z.B. UserId → "user_id"
//
// QUELLEN:
//   Entity Framework Core – DbContext:
//   https://learn.microsoft.com/ef/core/dbcontext-configuration/
//
//   EF Core – Pomelo MySQL Provider:
//   https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql
//
//   EF Core – Fluent API (OnModelCreating):
//   https://learn.microsoft.com/ef/core/modeling/
//
//   EF Core – Column Name Mapping (HasColumnName):
//   https://learn.microsoft.com/ef/core/modeling/entity-properties#column-names
//
//   EF Core – Table Name Mapping (ToTable):
//   https://learn.microsoft.com/ef/core/modeling/entity-properties#table-name
//
//   ServerVersion.AutoDetect (Pomelo):
//   https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql/blob/master/README.md
// ============================================================

using Microsoft.EntityFrameworkCore;
using Smartpantry.Helpers;
using Smartpantry.Models;

namespace SmartPantry2.Data
{
    class FoodDbContext : DbContext
    {
        // DbSet<T> repräsentiert eine Datenbanktabelle.
        // LINQ-Abfragen darauf werden von EF Core in SQL übersetzt.
        // Quelle: https://learn.microsoft.com/dotnet/api/microsoft.entityframeworkcore.dbset-1

        // → Tabelle "users" (gemappt in OnModelCreating)
        public DbSet<User> Users { get; set; }

        // → Tabelle "user_settings"
        public DbSet<UserSettings> UserSettings { get; set; }

        // → Tabelle "categories"
        public DbSet<Category> Categories { get; set; }

        // → Tabelle "food_items"
        public DbSet<FoodItem> FoodItems { get; set; }

        // → Tabelle "recipes"
        public DbSet<Recipe> Recipes { get; set; }

        // → Tabelle "recipe_ingredients"
        public DbSet<RecipeIngredient> RecipeIngredients { get; set; }

        // → Tabelle "meal_plan"
        public DbSet<MealPlan> MealPlans { get; set; }

        // → Tabelle "shopping_list"
        // (Name "ShoppingList" im Code, aber Tabelle heißt "shopping_list")
        public DbSet<ShoppingItem> ShoppingList { get; set; }

        // --------------------------------------------------------
        // OnConfiguring
        //
        // FUNKTION:
        //   Wird automatisch von EF Core beim ersten Datenbankzugriff
        //   aufgerufen. Konfiguriert welche Datenbank und welcher
        //   Provider (hier: MySQL via Pomelo) verwendet wird.
        //
        // FLOW:
        //   DatabaseConfigHelper.GetConnectionString()
        //     → liest App.config → gibt MySQL-Verbindungsstring zurück
        //   options.UseMySql(...)
        //     → teilt EF Core mit: "Nutze MySQL mit diesem String"
        //   ServerVersion.AutoDetect(connectionString)
        //     → Pomelo erkennt MySQL-Version automatisch per Verbindung
        //     → verhindert Inkompatibilitäten bei verschiedenen MySQL-Versionen
        //
        // DbContextOptionsBuilder: konfiguriert den DbContext
        // Quelle: https://learn.microsoft.com/dotnet/api/microsoft.entityframeworkcore.dbcontextoptionsbuilder
        // --------------------------------------------------------
        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            // Verbindungsstring aus App.config laden
            string connectionString = DatabaseConfigHelper.GetConnectionString();

            // Pomelo MySQL Provider konfigurieren
            // UseMySql: Erweiterungsmethode von Pomelo.EntityFrameworkCore.MySql
            // Quelle: https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql
            options.UseMySql(
                connectionString,
                // AutoDetect verbindet kurz zur DB um die Version zu ermitteln
                // → funktioniert auch ohne explizite Versionsangabe
                ServerVersion.AutoDetect(connectionString)
            );
        }

        // --------------------------------------------------------
        // OnModelCreating
        //
        // FUNKTION:
        //   Fluent API zum Konfigurieren des Datenbankschemas.
        //   Mappt C#-Propertynamen auf die tatsächlichen SQL-Spaltennamen.
        //
        // WARUM NÖTIG?
        //   C# nutzt PascalCase (ExpiryDate), SQL nutzt snake_case (expiration_date).
        //   Ohne Mapping würde EF Core nach "ExpiryDate" in der DB suchen
        //   → Spalte existiert nicht → Exception!
        //
        // ModelBuilder: API zum Konfigurieren des EF-Core-Modells
        // Quelle: https://learn.microsoft.com/dotnet/api/microsoft.entityframeworkcore.modelbuilder
        //
        // Entity<T>(e => { ... }): Konfiguriert eine spezifische Entity
        // ToTable(): legt den Tabellennamen fest
        // HasColumnName(): mappt Property auf Spaltenname
        // --------------------------------------------------------
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // ── TABELLE: users ──────────────────────────────────
            modelBuilder.Entity<User>(e =>
            {
                // Tabelle in der DB heißt "users"
                e.ToTable("users");
                // Alle Spaltennamen explizit mappen (C# PascalCase → SQL snake_case)
                e.Property(p => p.Id).HasColumnName("id");
                e.Property(p => p.Username).HasColumnName("username");
                e.Property(p => p.Email).HasColumnName("email");
                e.Property(p => p.PasswordHash).HasColumnName("password_hash");
                e.Property(p => p.Role).HasColumnName("role");
                e.Property(p => p.CreatedAt).HasColumnName("created_at");
            });

            // ── TABELLE: user_settings ──────────────────────────
            modelBuilder.Entity<UserSettings>(e =>
            {
                e.ToTable("user_settings");
                e.Property(p => p.Id).HasColumnName("id");
                e.Property(p => p.UserId).HasColumnName("user_id");
                e.Property(p => p.Theme).HasColumnName("theme");
                e.Property(p => p.Language).HasColumnName("language");
            });

            // ── TABELLE: categories ─────────────────────────────
            modelBuilder.Entity<Category>(e =>
            {
                e.ToTable("categories");
                e.Property(p => p.Id).HasColumnName("id");
                e.Property(p => p.Name).HasColumnName("name");
            });

            // ── TABELLE: food_items ─────────────────────────────
            modelBuilder.Entity<FoodItem>(e =>
            {
                e.ToTable("food_items");
                e.Property(p => p.Id).HasColumnName("id");
                e.Property(p => p.UserId).HasColumnName("user_id");
                e.Property(p => p.Name).HasColumnName("name");
                e.Property(p => p.Amount).HasColumnName("amount");
                e.Property(p => p.Unit).HasColumnName("unit");
                // ACHTUNG: ExpiryDate ↔ "expiration_date" (unterschiedliche Namen!)
                e.Property(p => p.ExpiryDate).HasColumnName("expiration_date");
                e.Property(p => p.CategoryId).HasColumnName("category_id");
                e.Property(p => p.CreatedAt).HasColumnName("created_at");
            });

            // ── TABELLE: recipes ────────────────────────────────
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

            // ── TABELLE: recipe_ingredients ─────────────────────
            modelBuilder.Entity<RecipeIngredient>(e =>
            {
                e.ToTable("recipe_ingredients");
                e.Property(p => p.Id).HasColumnName("id");
                e.Property(p => p.RecipeId).HasColumnName("recipe_id");
                // ACHTUNG: FoodItem (C#) ↔ "food_item_name" (DB) – unterschiedliche Namen!
                e.Property(p => p.FoodItem).HasColumnName("food_item_name");
                e.Property(p => p.Amount).HasColumnName("amount");
                e.Property(p => p.Unit).HasColumnName("unit");
            });

            // ── TABELLE: meal_plan ──────────────────────────────
            modelBuilder.Entity<MealPlan>(e =>
            {
                e.ToTable("meal_plan");
                e.Property(p => p.Id).HasColumnName("id");
                e.Property(p => p.UserId).HasColumnName("user_id");
                e.Property(p => p.RecipeId).HasColumnName("recipe_id");
                e.Property(p => p.Date).HasColumnName("date");
                e.Property(p => p.MealType).HasColumnName("meal_type");
            });

            // ── TABELLE: shopping_list ──────────────────────────
            modelBuilder.Entity<ShoppingItem>(e =>
            {
                e.ToTable("shopping_list");
                e.Property(p => p.Id).HasColumnName("id");
                e.Property(p => p.UserId).HasColumnName("user_id");
                e.Property(p => p.Name).HasColumnName("name");
                e.Property(p => p.Amount).HasColumnName("amount");
                e.Property(p => p.Unit).HasColumnName("unit");
                // ACHTUNG: IsBought (C#) ↔ "checked" (DB) – unterschiedliche Namen!
                // "checked" ist ein SQL-Schlüsselwort, daher "IsBought" im C#-Code
                e.Property(p => p.IsBought).HasColumnName("checked");
            });
        }
    }
}