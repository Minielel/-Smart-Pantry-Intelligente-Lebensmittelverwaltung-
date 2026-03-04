using Smartpantry.Models;
using System;

namespace SmartPantry2.Services
{
    public static class UserSession
    {
        private static User? _currentUser;

        public static User? CurrentUser
        {
            get => _currentUser;
            set
            {
                if (_currentUser?.Id == value?.Id) return;
                _currentUser = value;
                CurrentUserChanged?.Invoke();
            }
        }

        public static int? CurrentUserId => CurrentUser?.Id;

        public static event Action? CurrentUserChanged;

        public static void Logout() => CurrentUser = null;
    }
}
