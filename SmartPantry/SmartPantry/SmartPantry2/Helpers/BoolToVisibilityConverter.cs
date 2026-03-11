// ------------------------------------------------------------
// Datei: BoolToVisibilityConverter.cs
//
// Beschreibung:
// Diese Datei enthält Hilfsklassen. Solche Klassen unterstützen das Projekt an vielen Stellen, ohne selbst eine eigene Fachfunktion zu sein.
//
// Hinweis fuer die Vorstellung:
// Wenn man diese Datei in der Schule erklaeren moechte, kann man sagen,
// dass sie einen bestimmten Baustein der App uebernimmt und dadurch hilft,
// die Anwendung klar zu strukturieren.
// ------------------------------------------------------------
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SmartPantry2.Helpers
{



    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool flag = value is bool b && b;
            bool invert = parameter is string s && s.Equals("Invert", StringComparison.OrdinalIgnoreCase);
            if (invert) flag = !flag;

            return flag ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not Visibility v) return false;
            bool flag = v == Visibility.Visible;
            bool invert = parameter is string s && s.Equals("Invert", StringComparison.OrdinalIgnoreCase);
            return invert ? !flag : flag;
        }
    }
}
