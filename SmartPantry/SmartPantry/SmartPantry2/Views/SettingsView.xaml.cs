// ------------------------------------------------------------
// Datei: SettingsView.xaml.cs
//
// Beschreibung:
// Diese Datei beschreibt die sichtbare Oberfläche. Hier wird festgelegt, welche Felder, Buttons und Listen der Benutzer im Fenster sieht.
//
// Hinweis fuer die Vorstellung:
// Wenn man diese Datei in der Schule erklaeren moechte, kann man sagen,
// dass sie einen bestimmten Baustein der App uebernimmt und dadurch hilft,
// die Anwendung klar zu strukturieren.
// ------------------------------------------------------------
using System.Windows.Controls;

namespace SmartPantry2.Views
{
    public partial class SettingsView : UserControl
    {
        public SettingsView()
        {
            InitializeComponent();
        }

        private void CurrentPwd_OnPasswordChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is Smartpantry.ViewModels.SettingsViewModel vm)
                vm.CurrentPassword = ((PasswordBox)sender).Password;
        }

        private void NewPwd_OnPasswordChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is Smartpantry.ViewModels.SettingsViewModel vm)
                vm.NewPassword = ((PasswordBox)sender).Password;
        }
    }
}
