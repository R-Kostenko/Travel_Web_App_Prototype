using Microsoft.AspNetCore.Components;
using Travel_App_Web.Models;

namespace Travel_App_Web.Services
{
    public class UserStateService
    {
        public User? User { get; private set; } = null;
        public Func<User?, Task>? OnUserUpdate { get; set; } = null;
        //public Action? OnLogOut { get; set; } = null;

        public void Update(User? user)
        {
            User = user;
            OnUserUpdate?.Invoke(User);
        }

        //public void LogOut()
        //{
        //    OnLogOut?.Invoke();
        //    Update(null);
        //}
    }
}
