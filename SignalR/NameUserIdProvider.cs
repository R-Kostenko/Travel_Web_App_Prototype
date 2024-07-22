using Microsoft.AspNetCore.SignalR;

namespace Travel_App_Web.SignalR
{
    public class NameUserIdProvider : IUserIdProvider
    {
        public string GetUserId(HubConnectionContext connection) => connection.User?.Identity?.Name;
    }
}
