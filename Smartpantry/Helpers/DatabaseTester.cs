using Smartpantry.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Smartpantry.Helpers
{
    class DatabaseTester
    {
        public static void TestConnection()
        {
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
