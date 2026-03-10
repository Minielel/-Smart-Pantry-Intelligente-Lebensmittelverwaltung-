// ============================================================
// Datei:   DatabaseConfigHelper.cs
// Schicht: Helper / Infrastruktur
//
// ZWECK:
//   Liest den MySQL-Verbindungsstring aus App.config aus.
//   Kapselt ConfigurationManager damit kein anderer Code
//   direkt von App.config abhängig ist.
//
// ROTER FADEN:
//   App.config
//     └─→ DatabaseConfigHelper.GetConnectionString()
//           └─→ FoodDbContext.OnConfiguring()
//                 └─→ options.UseMySql(connectionString)
//                       └─→ Entity Framework verbindet sich mit MySQL
//
// USER USECASE:
//   Passiert vollautomatisch beim ersten DB-Zugriff.
//   Der User merkt nichts davon – außer wenn die Verbindung
//   fehlschlägt (dann: Exception-Meldung).
//
// QUELLEN:
//   ConfigurationManager (System.Configuration):
//   https://learn.microsoft.com/dotnet/api/system.configuration.configurationmanager
//
//   ConnectionStringSettings:
//   https://learn.microsoft.com/dotnet/api/system.configuration.connectionstringsettings
//
//   App.config / Connection Strings in .NET:
//   https://learn.microsoft.com/dotnet/framework/data/adonet/connection-strings-and-configuration-files
// ============================================================

// System.Configuration für ConfigurationManager und ConnectionStringSettings
using System.Configuration;

namespace Smartpantry.Helpers
{
    // "static": keine Instanz nötig, direkt über Klassenname aufrufbar
    // Quelle: https://learn.microsoft.com/dotnet/csharp/programming-guide/classes-and-structs/static-classes
    public static class DatabaseConfigHelper
    {
        // --------------------------------------------------------
        // GetConnectionString
        //
        // FUNKTION:
        //   Liest den Verbindungsstring mit dem Namen "FoodManagerDb"
        //   aus dem <connectionStrings>-Abschnitt der App.config.
        //
        // RETURN:
        //   string → vollständiger MySQL-Verbindungsstring, z.B.:
        //   "server=mysql.pb.bib.de;uid=pbt3h24afa;pwd=...;database=..."
        //
        // WIRFT Exception wenn:
        //   Der Name "FoodManagerDb" in App.config nicht existiert.
        //   → App startet nicht, Fehlerdialog erscheint.
        //
        // AUFGERUFEN VON: FoodDbContext.OnConfiguring()
        // --------------------------------------------------------
        public static string GetConnectionString()
        {
            // ConfigurationManager.ConnectionStrings liest die <connectionStrings>
            // aus App.config. Der Indexer ["FoodManagerDb"] sucht nach dem
            // Eintrag mit name="FoodManagerDb".
            // Quelle: https://learn.microsoft.com/dotnet/api/system.configuration.configurationmanager.connectionstrings
            ConnectionStringSettings settings =
            ConfigurationManager.ConnectionStrings["FoodManagerDb"];

            // null-Prüfung: wenn der Eintrag in App.config fehlt
            // → sofort abbrechen mit verständlicher Fehlermeldung
            if (settings == null)
                throw new Exception("Database connection string 'FoodManagerDb' not found in App.config");

            // .ConnectionString gibt nur den Verbindungsstring-Wert zurück
            // (ohne den Namen und providerName)
            return settings.ConnectionString;
        }
    }
}