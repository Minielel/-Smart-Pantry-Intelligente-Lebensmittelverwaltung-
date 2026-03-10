// ============================================================
// Datei:   RelayCommand.cs
// Schicht: Helper / MVVM-Infrastruktur
//
// ZWECK:
//   Implementiert ICommand für parameterlose Befehle.
//   Verbindet XAML-Buttons mit Methoden im ViewModel,
//   ohne Code-Behind in .xaml.cs schreiben zu müssen.
//
// ROTER FADEN:
//   In jedem ViewModel:
//     AddCommand = new RelayCommand(Add, CanAdd);
//   Im XAML:
//     <Button Command="{Binding AddCommand}"/>
//   WPF → CanExecute() → Button aktiv/ausgegraut
//   Klick → Execute() → Add() wird aufgerufen
//
// USER USECASE:
//   User gibt im Food-Formular einen Namen ein.
//   → Name-Setter ruft AddCommand.RaiseCanExecuteChanged() auf
//   → WPF prüft CanAdd() neu → true → "Hinzufügen"-Button wird aktiv
//   User klickt "Hinzufügen"
//   → RelayCommand.Execute() → FoodViewModel.Add()
//
// QUELLEN:
//   ICommand Interface (Microsoft Learn):
//   https://learn.microsoft.com/dotnet/api/system.windows.input.icommand
//
//   RelayCommand Pattern (MVVM):
//   https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/relaycommand
//
//   Action-Delegate (parameterlose Aktion):
//   https://learn.microsoft.com/dotnet/api/system.action
//
//   Func<bool>-Delegate (Funktion die bool zurückgibt):
//   https://learn.microsoft.com/dotnet/api/system.func-1
//
//   ArgumentNullException:
//   https://learn.microsoft.com/dotnet/api/system.argumentnullexception
// ============================================================

using System;
using System.Windows.Input;

namespace Smartpantry.Helpers
{
    // ICommand: WPF-Standard-Interface für alle Befehle
    // Quelle: https://learn.microsoft.com/dotnet/api/system.windows.input.icommand
    public class RelayCommand : ICommand
    {
        // Action-Delegate: hält die auszuführende Methode (z.B. Add, Delete)
        // "readonly": wird nach dem Konstruktor nie mehr geändert
        private readonly Action _execute;

        // Func<bool>-Delegate: hält die optionale Bedingungsprüfung (z.B. CanAdd)
        // "?" = nullable: wenn null, ist der Button immer aktiv
        private readonly Func<bool>? _canExecute;

        // --------------------------------------------------------
        // Konstruktor
        //
        // PARAMETER:
        //   execute    → die Methode die beim Button-Klick ausgeführt wird
        //                z.B. () => Add() oder einfach Add
        //   canExecute → optionale Methode die bestimmt ob Button aktiv ist
        //                z.B. () => !string.IsNullOrWhiteSpace(Name)
        //
        // "??" = Null-Coalescing-Operator mit throw:
        //   Wenn execute null ist → sofort ArgumentNullException werfen
        //   (Programmierfehler früh erkennen)
        //   Quelle: https://learn.microsoft.com/dotnet/csharp/language-reference/operators/null-coalescing-operator
        // --------------------------------------------------------
        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        // --------------------------------------------------------
        // CanExecute
        //
        // FUNKTION: von WPF aufgerufen um Button-Zustand zu bestimmen
        //
        // RETURN:
        //   true  → Button ist klickbar (normal dargestellt)
        //   false → Button ist ausgegraut und nicht klickbar
        //
        // "?." = Null-conditional: wenn _canExecute null → true (immer aktiv)
        // "??" = Null-Coalescing: falls Invoke() null liefert → true
        // Quelle: https://learn.microsoft.com/dotnet/csharp/language-reference/operators/member-access-operators#null-conditional-operators--and-
        // --------------------------------------------------------
        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

        // --------------------------------------------------------
        // Execute
        //
        // FUNKTION: von WPF aufgerufen wenn User den Button klickt
        //
        // Delegiert direkt an die im Konstruktor übergebene Methode.
        // parameter wird ignoriert (parameterlose Version)
        // --------------------------------------------------------
        public void Execute(object? parameter) => _execute();

        // WPF abonniert dieses Event intern.
        // Wenn es gefeuert wird → WPF ruft CanExecute() neu auf
        // → Button wird neu gezeichnet (aktiv/inaktiv)
        public event EventHandler? CanExecuteChanged;

        // --------------------------------------------------------
        // RaiseCanExecuteChanged
        //
        // FUNKTION:
        //   Löst manuell das CanExecuteChanged-Event aus.
        //   Wird aus ViewModels aufgerufen wenn sich Bedingungen ändern.
        //
        // BEISPIEL (in FoodViewModel):
        //   public string Name {
        //       set { if (SetProperty(ref _name, value))
        //                 AddCommand.RaiseCanExecuteChanged(); }
        //   }
        //   → Name wurde geändert → WPF prüft CanAdd() neu
        //   → Button wird aktiv sobald Name nicht mehr leer ist
        //
        // EventArgs.Empty: kein Datenpayload nötig, nur Signal
        // Quelle: https://learn.microsoft.com/dotnet/api/system.eventargs.empty
        // --------------------------------------------------------
        public void RaiseCanExecuteChanged() =>
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}