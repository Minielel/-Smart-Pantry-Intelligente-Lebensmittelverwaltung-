// ------------------------------------------------------------
// Datei: ResourceSwapService.cs
//
// Beschreibung:
// Diese Datei gehört zur Service-Schicht. Hier werden Aufgaben wie Datenbankzugriffe, Hilfslogik oder das Nachladen von Daten erledigt.
//
// Hinweis fuer die Vorstellung:
// Wenn man diese Datei in der Schule erklaeren moechte, kann man sagen,
// dass sie einen bestimmten Baustein der App uebernimmt und dadurch hilft,
// die Anwendung klar zu strukturieren.
// ------------------------------------------------------------
using System;
using System.Linq;
using System.Windows;

namespace SmartPantry2.Services
{
    public static class ResourceSwapService
    {
        public static void SwapMergedDictionary(string keyPrefix, Uri newDictionary)
        {

            var app = Application.Current;
            if (app == null) return;

            var merged = app.Resources.MergedDictionaries;
            var existing = merged.FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains(keyPrefix, StringComparison.OrdinalIgnoreCase));

            var dict = new ResourceDictionary { Source = newDictionary };

            if (existing != null)
            {
                var idx = merged.IndexOf(existing);
                merged[idx] = dict;
            }
            else
            {
                merged.Add(dict);
            }
        }
    }
}
