using SmartPantry2.Data;
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
