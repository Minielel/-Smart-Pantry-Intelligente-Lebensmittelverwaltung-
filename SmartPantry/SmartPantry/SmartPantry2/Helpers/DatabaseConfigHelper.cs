// ------------------------------------------------------------
// Datei: DatabaseConfigHelper.cs
//
// Beschreibung:
// Diese Datei enthält Hilfsklassen. Solche Klassen unterstützen das Projekt an vielen Stellen, ohne selbst eine eigene Fachfunktion zu sein.
//
// Hinweis fuer die Vorstellung:
// Wenn man diese Datei in der Schule erklaeren moechte, kann man sagen,
// dass sie einen bestimmten Baustein der App uebernimmt und dadurch hilft,
// die Anwendung klar zu strukturieren.
// ------------------------------------------------------------
﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smartpantry.Helpers
{
    public static class DatabaseConfigHelper
    {

        public static string GetConnectionString()
        {


            ConnectionStringSettings settings =
            ConfigurationManager.ConnectionStrings["FoodManagerDb"];

            if (settings == null)
                throw new Exception("Database connection string 'FoodManagerDb' not found in App.config");

            return settings.ConnectionString;
        }
    }
}
