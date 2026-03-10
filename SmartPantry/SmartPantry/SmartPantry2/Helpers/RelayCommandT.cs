// ============================================================
// Datei:   RelayCommand<T>.cs
// Schicht: Helper / MVVM-Infrastruktur
//
// ZWECK:
//   Typisierte Version von RelayCommand.
//   Wird benötigt wenn ein Button ein konkretes Datenobjekt
//   als Parameter übergeben muss (z.B. das zu löschende Item).
//
// ROTER FADEN:
//   In FoodViewModel:
//     DeleteFoodItemCommand = new RelayCommand<FoodItem>(DeleteFromTile, item => CanEdit && item != null);
//   Im XAML innerhalb eines ItemTemplates:
//     <Button Command="{Binding DataContext.DeleteFoodItemCommand,
//                               RelativeSource={RelativeSource AncestorType=ListBox}}"
//             CommandParameter="{Binding}"/>
//   → "{Binding}" ohne Pfad = das aktuelle ListBox-Item (FoodItem)
//   → WPF ruft Execute(foodItem) → DeleteFromTile(foodItem) wird aufgerufen
//
// GLEICHES MUSTER VERWENDET BEI:
//   - DeleteRecipeTileCommand (RecipesViewModel) → Recipe als Parameter
//   - DeleteItemCommand (ShoppingListViewModel)  → ShoppingItem als Parameter
//   - AddToFoodCommand (ShoppingListViewModel)   → ShoppingItem als Parameter
//   - ChooseFoodCommand (FoodViewModel)          → FoodItem als Parameter
//
// USER USECASE:
//   User klickt "×"-Button auf einer Food-Kachel
//   → CommandParameter = das FoodItem dieser Kachel
//   → Execute(foodItem) → DeleteFromTile(foodItem)
//   → Genau dieses eine Item wird aus der DB gelöscht
//
// QUELLEN:
//   Generic RelayCommand / Typed Commands (Microsoft Community Toolkit):
//   https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/relaycommand
//
//   Generics in C#:
//   https://learn.microsoft.com/dotnet/csharp/programming-guide/generics/
//
//   ICommand Interface:
//   https://learn.microsoft.com/dotnet/api/system.windows.input.icommand
//
//   Pattern Matching mit "is T t":
//   https://learn.microsoft.com/dotnet/csharp/language-reference/operators/patterns
// ============================================================

using System;
using System.Windows.Input;

namespace Smartpantry.Helpers
{
    // <T> = Typparameter: steht für den Typ des Command-Parameters
    // z.B. RelayCommand<FoodItem>, RelayCommand<Recipe>, RelayCommand<string>
    public class RelayCommand<T> : ICommand
    {
        // Action<T?>: Delegate für eine Methode die T als Parameter nimmt
        // "?" = T kann null sein (für Referenztypen)
        private readonly Action<T?> _execute;

        // Func<T?, bool>: Delegate für eine Methode die T nimmt und bool zurückgibt
        // Optional (nullable): wenn null → Button immer aktiv
        private readonly Func<T?, bool>? _canExecute;

        public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
        {
            // Null-Check: execute darf nicht null sein (Programmierfehler)
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        // --------------------------------------------------------
        // CanExecute
        //
        // RETURN: true = Button aktiv, false = ausgegraut
        //
        // TYPKONVERTIERUNG:
        //   WPF übergibt parameter als "object?" (untypisiert).
        //   "parameter is T t" = Pattern Matching:
        //     → wenn parameter vom Typ T ist: in Variable t speichern und nutzen
        //     → wenn nicht (z.B. null oder falscher Typ): default(T) verwenden
        //       (für Referenztypen = null)
        //   Quelle: https://learn.microsoft.com/dotnet/csharp/language-reference/operators/patterns
        // --------------------------------------------------------
        public bool CanExecute(object? parameter)
        {
            if (_canExecute == null) return true;

            // Versuche parameter zu T zu casten
            if (parameter is T t) return _canExecute(t);
            // Fallback: default(T) = null für Klassen, 0 für int usw.
            return _canExecute(default);
        }

        // --------------------------------------------------------
        // Execute
        //
        // FUNKTION: wird bei Button-Klick von WPF aufgerufen
        //
        // Castet WPF-parameter (object?) zu T und ruft _execute auf.
        // Wenn Cast fehlschlägt → default(T) wird übergeben.
        // --------------------------------------------------------
        public void Execute(object? parameter)
        {
            if (parameter is T t) _execute(t);
            else _execute(default);
        }

        // WPF abonniert dieses Event für Button-Status-Updates
        public event EventHandler? CanExecuteChanged;

        // Teilt WPF mit: "CanExecute neu prüfen"
        // Wird aufgerufen wenn sich CanEdit oder das Item selbst ändert
        public void RaiseCanExecuteChanged() =>
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}