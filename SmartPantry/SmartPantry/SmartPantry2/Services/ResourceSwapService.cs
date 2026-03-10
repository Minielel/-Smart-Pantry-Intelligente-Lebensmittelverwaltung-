// ============================================================
// Datei:   ResourceSwapService.cs
// Schicht: Service / UI-Infrastruktur
//
// ZWECK:
//   Tauscht WPF-ResourceDictionaries zur Laufzeit aus.
//   Ermöglicht Theme- und Sprachwechsel ohne App-Neustart.
//
// ROTER FADEN:
//   SettingsViewModel.SetTheme("blue")
//     → ApplyThemeResources("blue")
//       → ResourceSwapService.SwapMergedDictionary("Theme.", Theme.Blue.xaml)
//         → WPF tauscht Farb-Dictionary sofort aus
//         → alle DynamicResource-Brushes aktualisieren sich
//
//   SettingsViewModel.SetLanguage("en")
//     → ApplyLanguageResources("en")
//       → ResourceSwapService.SwapMergedDictionary("Strings.", Strings.en.xaml)
//         → alle {DynamicResource Nav_Dashboard} usw. zeigen englische Texte
//
// USER USECASE:
//   User klickt "Blue" → Sofortiger Theme-Wechsel ohne Neustart
//   User klickt "English" → Alle Menüpunkte/Buttons auf Englisch
//
// QUELLEN:
//   WPF ResourceDictionary (Microsoft Learn):
//   https://learn.microsoft.com/dotnet/desktop/wpf/systems/xaml-resources-overview
//
//   WPF MergedDictionaries (dynamischer Austausch):
//   https://learn.microsoft.com/dotnet/desktop/wpf/systems/xaml-resources-merged-dictionaries
//
//   Application.Current.Resources:
//   https://learn.microsoft.com/dotnet/api/system.windows.application.resources
//
//   LINQ – FirstOrDefault() auf Collections:
//   https://learn.microsoft.com/dotnet/api/system.linq.enumerable.firstordefault
// ============================================================

using System;
using System.Linq;
using System.Windows;

namespace SmartPantry2.Services
{
    // "static": keine Instanz nötig, direkt über Klassenname aufrufbar
    public static class ResourceSwapService
    {
        // --------------------------------------------------------
        // SwapMergedDictionary
        //
        // FUNKTION:
        //   Sucht in Application.Resources.MergedDictionaries nach
        //   einem Dictionary dessen Source-Pfad den keyPrefix enthält.
        //   Ersetzt es durch das neue Dictionary.
        //   Wenn nicht gefunden → neues Dictionary hinzufügen.
        //
        // PARAMETER:
        //   keyPrefix     → Suchstring im Dictionary-Pfad
        //                   z.B. "Theme." findet "Resources/Theme.Green.xaml"
        //                   z.B. "Strings." findet "Resources/Strings.de.xaml"
        //   newDictionary → Uri zum neuen XAML-File
        //                   z.B. new Uri("Resources/Theme.Blue.xaml", UriKind.Relative)
        //
        // RETURN: void → WPF aktualisiert DynamicResource-Bindings automatisch
        //
        // WIE WIRKT ES?
        //   Alle Steuerelemente die {DynamicResource AccentBrush} verwenden
        //   bekommen automatisch den neuen Wert aus dem neuen Dictionary.
        //   {StaticResource} würde NICHT aktualisiert werden!
        //   Quelle: https://learn.microsoft.com/dotnet/desktop/wpf/systems/xaml-resources-dynamic-static-comparison
        // --------------------------------------------------------
        public static void SwapMergedDictionary(string keyPrefix, Uri newDictionary)
        {
            // Application.Current: die laufende WPF-App
            // Quelle: https://learn.microsoft.com/dotnet/api/system.windows.application.current
            var app = Application.Current;
            // App könnte null sein (z.B. in Unit-Tests)
            if (app == null) return;

            // MergedDictionaries: Liste aller geladenen ResourceDictionaries
            var merged = app.Resources.MergedDictionaries;

            // LINQ FirstOrDefault: suche Dictionary mit diesem Präfix im Pfad
            // d.Source?.OriginalString: null-safe Zugriff auf den Pfad-String
            // Contains(..., OrdinalIgnoreCase): Groß-/Kleinschreibung egal
            var existing = merged.FirstOrDefault(d =>
                d.Source != null &&
                d.Source.OriginalString.Contains(keyPrefix, StringComparison.OrdinalIgnoreCase));

            // Neues Dictionary erstellen und Source-Uri setzen
            // ResourceDictionary: Container für XAML-Ressourcen
            // Quelle: https://learn.microsoft.com/dotnet/api/system.windows.resourcedictionary
            var dict = new ResourceDictionary { Source = newDictionary };

            if (existing != null)
            {
                // Vorhandenes Dictionary an gleicher Position ersetzen
                // → alle anderen Dictionaries bleiben erhalten
                var idx = merged.IndexOf(existing);
                merged[idx] = dict;  // Ersetzen → WPF reagiert sofort
            }
            else
            {
                // Noch kein Dictionary mit diesem Präfix → einfach hinzufügen
                merged.Add(dict);
            }
        }
    }
}