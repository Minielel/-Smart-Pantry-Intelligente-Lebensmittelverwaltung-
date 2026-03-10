// ============================================================
// Datei:   DatabaseTester.cs
// Schicht: Helper / Entwicklungswerkzeug
//
// ZWECK:
//   Testet beim App-Start ob die Datenbankverbindung funktioniert.
//   Gibt sofortiges Feedback per MessageBox – nützlich in der
//   Entwicklungsphase um Verbindungsprobleme früh zu erkennen.
//
// ROTER FADEN:
//   MainWindow-Konstruktor
//     └─→ DatabaseTester.TestConnection()
//           └─→ new FoodDbContext()
//                 └─→ context.Database.CanConnect()
//                       → MessageBox: "successful" oder Fehlermeldung
//
// USER USECASE:
//   Nur für Entwickler/Deployment relevant.
//   Im Produktivbetrieb kann dieser Aufruf entfernt werden
//   um die unnötige MessageBox beim Start zu vermeiden.
//
// QUELLEN:
//   DatabaseFacade.CanConnect() (Entity Framework Core):
//   https://learn.microsoft.com/dotnet/api/microsoft.entityframeworkcore.infrastructure.databasefacade.canconnect
//
//   MessageBox.Show (WPF):
//   https://learn.microsoft.com/dotnet/api/system.windows.messagebox
//
//   using-Statement (automatisches Dispose):
//   https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/using-statement
// ============================================================

// FoodDbContext aus dem Data-Namespace
using SmartPantry2.Data;
using System;
using System.Windows;

namespace Smartpantry.Helpers
{
    public class DatabaseTester
    {
        // --------------------------------------------------------
        // TestConnection
        //
        // FUNKTION:
        //   Öffnet kurz einen DB-Kontext und prüft die Verbindung.
        //   Zeigt Ergebnis als Modal-Dialog (MessageBox).
        //
        // RETURN: void – Ergebnis wird als Dialog angezeigt.
        //
        // FEHLERBEHANDLUNG:
        //   try/catch fängt jeden Verbindungsfehler ab (z.B. Server
        //   nicht erreichbar, falsches Passwort) und zeigt die
        //   Exception-Meldung an statt die App abstürzen zu lassen.
        // --------------------------------------------------------
        public static void TestConnection()
        {
            // Ausgabe in die Debug-Konsole (sichtbar im VS Output-Fenster)
            // Quelle: https://learn.microsoft.com/dotnet/api/system.console.writeline
            Console.WriteLine("TestConnection() wurde aufgerufen.");
            try
            {
                // "using" stellt sicher dass der Kontext nach dem Block
                // automatisch disposed (Verbindung geschlossen) wird,
                // auch wenn eine Exception auftritt.
                // Quelle: https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/using-statement
                using (var context = new FoodDbContext())
                {
                    // CanConnect() führt eine einfache Verbindungsprüfung durch
                    // (kein vollständiges Query, nur Ping zur DB)
                    // Quelle: https://learn.microsoft.com/dotnet/api/microsoft.entityframeworkcore.infrastructure.databasefacade.canconnect
                    if (context.Database.CanConnect())
                        MessageBox.Show("Database connection successful!");
                    else
                        MessageBox.Show("Database connection failed.");
                }
            }
            catch (Exception ex)
            {
                // ex.Message enthält die konkrete Fehlerbeschreibung
                // (z.B. "Unable to connect to any of the specified MySQL hosts")
                // "\n" = Zeilenumbruch in der MessageBox
                MessageBox.Show("Error connecting to database:\n" + ex.Message);
            }
        }
    }
}