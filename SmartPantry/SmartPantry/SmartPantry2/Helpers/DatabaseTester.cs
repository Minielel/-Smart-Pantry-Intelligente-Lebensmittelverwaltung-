// ------------------------------------------------------------
// Datei: DatabaseTester.cs
//
// Beschreibung:
// Diese Datei enthält Hilfsklassen. Solche Klassen unterstützen das Projekt an vielen Stellen, ohne selbst eine eigene Fachfunktion zu sein.
//
// Hinweis fuer die Vorstellung:
// Wenn man diese Datei in der Schule erklaeren moechte, kann man sagen,
// dass sie einen bestimmten Baustein der App uebernimmt und dadurch hilft,
// die Anwendung klar zu strukturieren.
// ------------------------------------------------------------
﻿using SmartPantry2.Data;
using System;
using System.Windows;

namespace Smartpantry.Helpers
{
    public class DatabaseTester
    {
        public static void TestConnection()
        {
            Console.WriteLine("TestConnection() wurde aufgerufen.");
            try
            {
                using (var context = new FoodDbContext())
                {
                    if (context.Database.CanConnect())
                        MessageBox.Show("Database connection successful!");
                    else
                        MessageBox.Show("Database connection failed.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error connecting to database:\n" + ex.Message);
            }
        }
    }
}
