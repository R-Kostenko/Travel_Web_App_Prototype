using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Models;
using System.Collections.Concurrent;
using Travel_App_Web.Data;

namespace Services
{
    public class UserStateService
    {
        public User? User { get; private set; } = null;
        public Country? UserCountry { get; set; } = null;
        private Country UkraineByDefault { get; set; }
        public Func<User?, Task>? OnUserUpdate { get; set; } = null;
        public Func<Country?, Task>? OnCountryUpdate { get; set; } = null;
        public Action? OnAuthorizationRequest { get; set; } = null;
        public Action? OnAuthorizationDeny { get; set; } = null;

        private readonly DBContext _dBContext;
        private readonly HttpClient _httpClient;
        private readonly NavigationManager _navigationManager;


        public UserStateService(DBContext dBContext, HttpClient httpClient, NavigationManager navigationManager)
        {
            _dBContext = dBContext;
            _httpClient = httpClient;
            _navigationManager = navigationManager;

            // Sets Ukraine as the default country if it exists in the database
            UkraineByDefault = dBContext.Countries.Find("UA") ?? new();

            // Getting the user's IP address
            string ipAddress = GetUserIPAddress();

            // Search for a user in the database by IP address
            User = dBContext.Users
                            .Include(u => u.Country)
                            .Include(u => u.Role)
                            .FirstOrDefault(u => !string.IsNullOrEmpty(u.IPSString) && u.IPSString.Contains(ipAddress));

            if (User != null)
            {
                // Detach user object from context to avoid change conflicts
                dBContext.Entry(User).State = EntityState.Detached;

                // Execute a request to authenticate a user
                var response = _httpClient.GetAsync(_navigationManager.BaseUri + $"account/auth/{User.Email}-{User.Role.RoleName}").Result;

                // Setting the user's country and clearing the user's Country field
                UserCountry = User.Country;
                User.Country = null;
            }
            else
            {
                // If the user is not found, the default country is set
                UserCountry = UkraineByDefault;
            }
        }

        private string GetUserIPAddress()
        {
            var httpContextAccessor = new HttpContextAccessor();

            return httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? string.Empty;
        }

        public void Update(User? user)
        {
            CountryUpdate(user?.Country);

            if (user != null)
            {
                user.Country = null;

                string userIP = GetUserIPAddress();
                var reqUser = _dBContext.Users.Find(user.Email);
                if (!reqUser.IPS.Contains(userIP))
                {
                    reqUser.AddIP(userIP);
                    _dBContext.SaveChanges();
                    _dBContext.Entry(reqUser).State = EntityState.Detached;
                }
            }

            User = user;
            OnUserUpdate?.Invoke(User);
        }
        public void CountryUpdate(Country? country)
        {
            UserCountry = country;
            UserCountry ??= UkraineByDefault;
            OnCountryUpdate?.Invoke(UserCountry);
        }

        public void RequestAuthorization() => OnAuthorizationRequest?.Invoke();
        public void DenyAuthorization() => OnAuthorizationDeny?.Invoke();

        public async Task LogOut()
        {
            var logoutResponse = await _httpClient.GetAsync(_navigationManager.BaseUri + "account/logout");
            if (logoutResponse.IsSuccessStatusCode)
            {
                string userIP = GetUserIPAddress();
                if (User != null && User.IPS.Contains(userIP))
                {
                    var reqUser = await _dBContext.Users.FindAsync(User.Email);
                    reqUser.RemoveIP(userIP);
                    _dBContext.SaveChanges();
                }
                Update(null);
            }
        }
    }
}
