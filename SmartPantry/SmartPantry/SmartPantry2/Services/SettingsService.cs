// ============================================================
// Datei:   SettingsService.cs
// Schicht: Service / Einstellungsverwaltung
//
// ZWECK:
//   Liest und schreibt UserSettings (Theme + Sprache) in der DB.
//   Einfache CRUD-Operationen ohne besondere Logik.
//
// ROTER FADEN:
//   SettingsViewModel → SettingsService → DB: user_settings
//
//   LIFECYCLE:
//   1. AuthService.Register() → Add() → Standard-Settings anlegen
//   2. AuthService.Login() → Settings per Include() mitladen
//   3. SettingsViewModel.Load() → Get() → Theme + Sprache anwenden
//   4. SettingsViewModel.SaveSettings() → Update() → in DB persistieren
//
// QUELLEN:
//   EF Core – FirstOrDefault():
//   https://learn.microsoft.com/dotnet/api/system.linq.queryable.firstordefault
//
//   EF Core – Update() (Disconnected Entities):
//   https://learn.microsoft.com/ef/core/saving/disconnected-entities
// ============================================================

using Smartpantry.Models;
using SmartPantry2.Data;
using System.Linq;

namespace SmartPantry2.Services
{
    public class SettingsService
    {
        // --------------------------------------------------------
        // Get
        //
        // FUNKTION: Lädt UserSettings eines Users aus der DB
        //
        // RETURN:
        //   UserSettings-Objekt → Theme und Language können gelesen werden
        //   null → noch keine Settings vorhanden (bei alten Users möglich)
        //          → SettingsViewModel legt dann neue Settings an
        //
        // DB: SELECT * FROM user_settings WHERE user_id = ? LIMIT 1
        // --------------------------------------------------------
        public UserSettings? Get(int userId)
        {
            using var db = new FoodDbContext();
            // FirstOrDefault: gibt erstes Ergebnis oder null zurück
            // (sollte maximal einen Datensatz pro User geben, da 1:1 Beziehung)
            return db.UserSettings.FirstOrDefault(s => s.UserId == userId);
        }

        // --------------------------------------------------------
        // Add
        //
        // FUNKTION: Legt neuen UserSettings-Datensatz an
        //
        // AUFGERUFEN VON:
        //   AuthService.Register() → nach Registrierung (Standard-Werte)
        //   SettingsViewModel.SaveSettings() → wenn Settings noch null sind
        //
        // DB: INSERT INTO user_settings (user_id, theme, language)
        // --------------------------------------------------------
        public void Add(UserSettings settings)
        {
            using var db = new FoodDbContext();
            db.UserSettings.Add(settings);
            db.SaveChanges();
        }

        // --------------------------------------------------------
        // Update
        //
        // FUNKTION: Speichert geänderte Settings in der DB
        //
        // AUFGERUFEN VON: SettingsViewModel.SaveSettings()
        //   → nach Klick auf "Speichern" in SettingsView
        //
        // DB: UPDATE user_settings SET theme=?, language=? WHERE id=?
        // --------------------------------------------------------
        public void Update(UserSettings settings)
        {
            using var db = new FoodDbContext();
            // Update(): EF Core erkennt geänderte Properties und erzeugt UPDATE-SQL
            db.UserSettings.Update(settings);
            db.SaveChanges();
        }
    }
}