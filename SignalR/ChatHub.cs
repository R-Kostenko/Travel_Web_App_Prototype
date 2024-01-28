using Humanizer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Security.Claims;
using Travel_App_Web.Data;
using Travel_App_Web.Models;

namespace Travel_App_Web.SignalR
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly DBContext _dbContext;
        private static readonly ConcurrentDictionary<string, string> _usersOnline = new();
        private static readonly ConcurrentQueue<(User, string, string)> _serviceQueue = new();
        private readonly List<Chat> _activeChats = new();

        public ChatHub(DBContext dbContext) : base()
        {
            this._dbContext = dbContext;
            this._dbContext.ChangeTracker.LazyLoadingEnabled = false;
        }

        public override async Task OnConnectedAsync()
        {
            string? userEmail = Context.User?.Identity?.Name;
            string? role = Context.User?.FindFirst(ClaimTypes.Role)?.Value;

            if (!string.IsNullOrEmpty(userEmail) && !string.IsNullOrEmpty(role))
            {
                _usersOnline.TryAdd(userEmail, role);
            }

            if (role == "Admin" && _serviceQueue.Count > 0)
            {
                await ProcessAdminServiceAsync(userEmail);
            }

            if (_dbContext.Chats != null)
            {
                var userChats = _dbContext.Chats?
                    .Include(chat => chat.Messages)
                    .Where(chat => chat.InterlocutorsEmails.Any(einfo => einfo.Email == userEmail))
                    .ToList();

                if (userChats != null)
                {
                    foreach (var chat in userChats)
                    {
                        if (!_activeChats.Contains(chat))
                        {
                            _activeChats.Add(chat);
                        }
                    }
                }
            }

            await base.OnConnectedAsync();
        }

        private async Task ProcessAdminServiceAsync(string userEmail)
        {
            if (_dbContext.Users != null)
            {
                var admin = await _dbContext.Users
                .Include(a => a.Chats)
                .ThenInclude(chat => chat.Messages)
                .FirstOrDefaultAsync(a => a.Email == userEmail);

                if (_serviceQueue.TryDequeue(out var value))
                {
                    var (client, senderName, message) = value;

                    if (client != null && admin != null)
                    {
                        var chat = new Chat()
                        {
                            InterlocutorsEmails = new() { 
                                new EmailInfo() { Email = client.Email },
                                new EmailInfo() { Email = admin.Email }
                            },
                        };
                        chat.Messages.Add(new Message()
                        {
                            SenderEmail = client.Email,
                            Content = message
                        });

                        _dbContext.Chats.Add(chat);
                        admin.Chats.AddChat(chat, admin.Role);
                        client.Chats.AddChat(chat, client.Role);
                        await _dbContext.SaveChangesAsync();

                        _activeChats.Add(chat);
                        await Clients.User(admin.Email).SendAsync("Receive", message, senderName);
                    }
                }
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            string role;
            var userEmail = Context.User?.Identity?.Name;
            if (!string.IsNullOrEmpty(userEmail))
            {
                _usersOnline.TryRemove(userEmail, out role);
            }

            var userChats = _dbContext.Chats?
                .Include(chat => chat.Messages)
                .Where(chat => chat.InterlocutorsEmails.Any(einfo => einfo.Email == userEmail))
                .ToList();

            if (userChats != null)
            {
                await ProcessChatsToRemoveAsync(userChats, userEmail);
            }

            await base.OnDisconnectedAsync(exception);
        }

        private Task ProcessChatsToRemoveAsync(List<Chat> userChats, string userEmail)
        {
            return Task.Run(() =>
            {
                var chatsToRemove = userChats
                .Where(userChat => _activeChats.Contains(userChat) &&
                    !userChat.InterlocutorsEmails.Where(einfo => einfo.Email != userEmail)
                        .Any(einfo => _usersOnline.ContainsKey(einfo.Email)))
                .ToList();

                foreach (var chatToRemove in chatsToRemove)
                {
                    _activeChats.Remove(chatToRemove);
                }
            });
        }

        public async Task Send(string senderName, string message, string receiver = "")
        {
            if (string.IsNullOrEmpty(receiver))
            {
                User? user = _dbContext.Users
                    .Include(user => user.Chats)
                    .ThenInclude(chat => chat.Messages)
                    .FirstOrDefault(user => user.Email == Context.User.Identity.Name);
                
                if (user != null)
                {
                    _serviceQueue.Enqueue((user, senderName, message));
                }

                if (_usersOnline.Any(user => user.Value == "Admin"))
                {
                    var adminsId = _usersOnline.Where(user => user.Value == "Admin").Select(user => user.Key);
                    var admin = await _dbContext.Users
                        .Include(user => user.Chats)
                        .ThenInclude(chat => chat.Messages)
                        .Include(user => user.Role)
                        .Where(user => adminsId.Contains(user.Email))
                        .OrderBy(admin => admin.Chats.Count).FirstAsync();

                    await ProcessAdminServiceAsync(admin.Email);
                }
            }
            else
            {
                var chat = _activeChats.Find(chat => chat.InterlocutorsEmails.Count == 2 &&
                    chat.InterlocutorsEmails.Exists(einfo => einfo.Email == receiver) && 
                    chat.InterlocutorsEmails.Exists(einfo => einfo.Email == Context.User.Identity.Name));

                if (chat != null)
                {
                    chat.Messages.Add(new Message()
                    {
                        SenderEmail = Context.User.Identity.Name,
                        Content = message,
                    });

                    await _dbContext.SaveChangesAsync();
                    await Clients.User(receiver).SendAsync("Receive", message, senderName);
                }
            }
        }
    }
}
