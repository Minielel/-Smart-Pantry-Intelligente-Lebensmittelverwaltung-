// ============================================================
// Datei:   BoolToVisibilityConverter.cs
// Schicht: Helper / UI-Konverter
//
// ZWECK:
//   WPF Value Converter: wandelt einen bool-Wert in
//   Visibility.Visible oder Visibility.Collapsed um.
//   Ermöglicht das Ein-/Ausblenden von UI-Elementen direkt
//   per XAML-Binding, ohne Code-Behind zu schreiben.
//
// ROTER FADEN:
//   In Views wird CanEdit (bool aus ViewModel) genutzt,
//   um Admin-Only-Buttons ein-/auszublenden:
//     Visibility="{Binding CanEdit, Converter={StaticResource BoolToVisibility}}"
//   Standard-User → CanEdit=false → Buttons sind Collapsed (unsichtbar + kein Platz)
//   Admin-User    → CanEdit=true  → Buttons sind Visible
//
// USER USECASE:
//   Standard-User loggt sich ein:
//   → CanEdit = false (UserSession.IsAdmin = false)
//   → Converter wandelt false → Visibility.Collapsed
//   → Hinzufügen/Löschen-Buttons verschwinden
//
// QUELLEN:
//   IValueConverter Interface (Microsoft Learn):
//   https://learn.microsoft.com/dotnet/api/system.windows.data.ivalueconverter
//
//   WPF Visibility Enum:
//   https://learn.microsoft.com/dotnet/api/system.windows.visibility
//
//   WPF Data Binding Converters:
//   https://learn.microsoft.com/dotnet/desktop/wpf/data/how-to-convert-bound-data
//
//   StringComparison.OrdinalIgnoreCase (Groß-/Kleinschreibung ignorieren):
//   https://learn.microsoft.com/dotnet/api/system.stringcomparison
// ============================================================

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SmartPantry2.Helpers
{
    // IValueConverter: Interface für WPF-Datenkonverter
    // Quelle: https://learn.microsoft.com/dotnet/api/system.windows.data.ivalueconverter
    public class BoolToVisibilityConverter : IValueConverter
    {
        // --------------------------------------------------------
        // Convert  (Richtung: ViewModel → View)
        //
        // FUNKTION:
        //   Wandelt bool → Visibility.
        //   Wird von WPF aufgerufen wenn sich der gebundene bool-Wert ändert.
        //
        // PARAMETER (alle von WPF übergeben):
        //   value     → der bool-Wert aus dem ViewModel (z.B. CanEdit)
        //   targetType → Zieltyp (hier Visibility, wird nicht direkt genutzt)
        //   parameter → optionaler String "Invert" → kehrt die Logik um
        //               Nutzung: ConverterParameter=Invert
        //   culture   → Kulturinfo (hier nicht genutzt)
        //
        // RETURN:
        //   Visibility.Visible   → Element wird angezeigt
        //   Visibility.Collapsed → Element ist unsichtbar UND nimmt keinen Platz weg
        //                          (Unterschied zu Visibility.Hidden: Hidden nimmt Platz)
        // --------------------------------------------------------
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // "is bool b" = Pattern Matching: prüft ob value ein bool ist
            // und weist ihn gleichzeitig der Variable b zu.
            // Quelle: https://learn.microsoft.com/dotnet/csharp/language-reference/operators/is
            bool flag = value is bool b && b;

            // Prüfen ob der "Invert"-Parameter gesetzt ist
            // → damit kann man sagen: "zeige wenn NICHT eingeloggt"
            bool invert = parameter is string s && s.Equals("Invert", StringComparison.OrdinalIgnoreCase);

            // Wenn Invert → Logik umkehren
            if (invert) flag = !flag;

            // Ternärer Operator: flag ? Sichtbar : Unsichtbar
            // Quelle Visibility: https://learn.microsoft.com/dotnet/api/system.windows.visibility
            return flag ? Visibility.Visible : Visibility.Collapsed;
        }

        // --------------------------------------------------------
        // ConvertBack  (Richtung: View → ViewModel)
        //
        // FUNKTION:
        //   Umgekehrte Konvertierung: Visibility → bool.
        //   Wird für Two-Way-Bindings benötigt (selten bei Visibility).
        //   In SmartPantry hauptsächlich für Vollständigkeit implementiert.
        // --------------------------------------------------------
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // "is not Visibility v" = Negiertes Pattern Matching (C# 9+)
            // Wenn value kein Visibility-Wert ist → false zurückgeben
            if (value is not Visibility v) return false;

            // Visible = true, alles andere (Collapsed/Hidden) = false
            bool flag = v == Visibility.Visible;

            bool invert = parameter is string s && s.Equals("Invert", StringComparison.OrdinalIgnoreCase);
            return invert ? !flag : flag;
        }
    }
}