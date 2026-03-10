// ============================================================
// Datei:   BaseViewModel.cs
// Schicht: Helper / Basis-Infrastruktur
//
// ZWECK:
//   Basisklasse für ALLE ViewModels im MVVM-Muster.
//   Kapselt INotifyPropertyChanged, damit WPF-Bindings
//   automatisch auf Datenänderungen reagieren.
//
// ROTER FADEN:
//   Jedes ViewModel erbt von BaseViewModel:
//     FoodViewModel : BaseViewModel
//     LoginViewModel : BaseViewModel  usw.
//   Ohne diese Klasse müsste jedes ViewModel PropertyChanged
//   selbst implementieren → Codeduplizierung.
//
// USER USECASE:
//   User tippt in ein Textfeld (z.B. Name im Food-Formular).
//   Das Textfeld ist per {Binding Name} ans ViewModel gebunden.
//   SetProperty() erkennt die Änderung → OnPropertyChanged() →
//   WPF liest neuen Wert und zeigt ihn sofort an.
//
// QUELLEN:
//   INotifyPropertyChanged (Microsoft Learn):
//   https://learn.microsoft.com/dotnet/api/system.componentmodel.inotifypropertychanged
//
//   CallerMemberName-Attribut (vermeidet Magic-Strings wie "Name"):
//   https://learn.microsoft.com/dotnet/api/system.runtime.compilerservices.callermembernameattribute
//
//   MVVM-Pattern Übersicht (Microsoft Learn):
//   https://learn.microsoft.com/dotnet/architecture/maui/mvvm
//
//   EqualityComparer<T>.Default (Wertvergleich ohne Boxing):
//   https://learn.microsoft.com/dotnet/api/system.collections.generic.equalitycomparer-1.default
// ============================================================

// System.Collections.Generic für EqualityComparer<T>
using System.Collections.Generic;
// System.ComponentModel für INotifyPropertyChanged und PropertyChangedEventArgs
using System.ComponentModel;
// System.Runtime.CompilerServices für [CallerMemberName]
using System.Runtime.CompilerServices;

namespace Smartpantry.Helpers
{
    // Alle ViewModels erben von dieser Klasse.
    // "public" damit alle Projekte/Namespaces darauf zugreifen können.
    public class BaseViewModel : INotifyPropertyChanged
    {
        // Dieses Event wird von WPF intern abonniert.
        // Sobald es gefeuert wird, liest WPF den neuen Wert aus
        // dem ViewModel und aktualisiert das gebundene UI-Element.
        // "?" = nullable: Event kann null sein wenn niemand abonniert hat.
        public event PropertyChangedEventHandler? PropertyChanged;

        // --------------------------------------------------------
        // SetProperty<T>
        //
        // FUNKTION:
        //   Setzt einen privaten Backing-Field-Wert und benachrichtigt
        //   WPF nur dann, wenn sich der Wert wirklich geändert hat.
        //   Verhindert unnötige UI-Refreshes.
        //
        // GENERISCHER TYP <T>:
        //   Funktioniert für string, int, decimal, bool, Objekte usw.
        //   Quelle: https://learn.microsoft.com/dotnet/csharp/programming-guide/generics/
        //
        // PARAMETER:
        //   ref T field       → das private Backing-Field (z.B. _name)
        //                       "ref" = Übergabe per Referenz, damit wir
        //                       den Wert direkt im Feld setzen können
        //                       https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/ref
        //   T value           → der neue Wert (kommt vom UI-Binding)
        //   propertyName      → Name der Property als String.
        //                       [CallerMemberName] befüllt das automatisch
        //                       mit dem Namen der aufrufenden Property!
        //                       Quelle: https://learn.microsoft.com/dotnet/api/system.runtime.compilerservices.callermembernameattribute
        //
        // RETURN:
        //   true  → Wert hat sich geändert, PropertyChanged wurde gefeuert
        //   false → Wert war identisch, kein Update nötig
        //
        // TYPISCHES AUFRUFMUSTER in jedem ViewModel:
        //   private string _name = "";
        //   public string Name {
        //       get => _name;
        //       set {
        //           if (SetProperty(ref _name, value))   // ← hier
        //               AddCommand.RaiseCanExecuteChanged();
        //       }
        //   }
        // --------------------------------------------------------
        protected bool SetProperty<T>(
            ref T field,
            T value,
            [CallerMemberName] string? propertyName = null)
        {
            // EqualityComparer<T>.Default: typsicherer Wertvergleich.
            // Für string → Stringvergleich, für int → Zahlenvergleich usw.
            // Verhindert unnötige PropertyChanged-Events wenn Wert gleich bleibt.
            // Quelle: https://learn.microsoft.com/dotnet/api/system.collections.generic.equalitycomparer-1
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            // Wert in das Backing-Field schreiben (per ref-Parameter)
            field = value;

            // WPF über die Änderung informieren
            OnPropertyChanged(propertyName);
            return true;
        }

        // --------------------------------------------------------
        // OnPropertyChanged
        //
        // FUNKTION:
        //   Feuert das PropertyChanged-Event direkt.
        //   Wird auch manuell aufgerufen wenn mehrere Properties
        //   auf einmal aktualisiert werden sollen ohne SetProperty.
        //
        // BEISPIEL-AUFRUF:
        //   OnPropertyChanged(nameof(CanEdit));
        //   → WPF prüft alle Buttons die an CanEdit gebunden sind
        //     und zeichnet sie neu (aktiv/inaktiv/sichtbar)
        //
        // "nameof()" gibt den Property-Namen als String zurück,
        // verhindert Tippfehler bei Magic-Strings.
        // Quelle: https://learn.microsoft.com/dotnet/csharp/language-reference/operators/nameof
        // --------------------------------------------------------
        protected void OnPropertyChanged(string? propertyName = null)
        {
            // "?." = Null-conditional Operator: feuert nur wenn jemand abonniert hat
            // "this" = der aktuelle ViewModel als Sender
            // PropertyChangedEventArgs transportiert den Property-Namen zu WPF
            // Quelle: https://learn.microsoft.com/dotnet/api/system.componentmodel.propertychangedeventargs
            PropertyChanged?.Invoke(this,
                new PropertyChangedEventArgs(propertyName));
        }
    }
}