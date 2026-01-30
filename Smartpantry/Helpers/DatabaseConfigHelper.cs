using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smartpantry.Helpers
{
    public static class DatabaseConfigHelper
    {
        // Returns the main database connection string
        public static string GetConnectionString()
        {
            // Read connection string by name from App.config
            // https://learn.microsoft.com/en-us/dotnet/framework/data/adonet/connection-strings-and-configuration-files?utm_source=chatgpt.com
            ConnectionStringSettings settings =
            ConfigurationManager.ConnectionStrings["FoodManagerDb"];

            if (settings == null)
                throw new Exception("Database connection string 'FoodManagerDb' not found in App.config");

            return settings.ConnectionString;
        }
    }
}
