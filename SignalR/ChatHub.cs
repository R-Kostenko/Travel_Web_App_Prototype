using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Concurrent;
using System.Security.Claims;
using Travel_App_Web.Data;
using Travel_App_Web.Models;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore.Storage;

namespace Travel_App_Web.SignalR
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly DBContext _dbContext;
        private static readonly ConcurrentDictionary<string, string> _usersOnline = new();
        private static readonly ConcurrentQueue<(User, string, string)> _serviceQueue = new();
        private static readonly ActiveChatList _activeChats = new();
        private sealed class ActiveChatList
        {
            private readonly List<Chat> _activeChats = new List<Chat>();
            private readonly object _lock = new object();

            public void AddChat(Chat chat)
            {
                lock (_lock)
                {
                    _activeChats.Add(chat);
                }
            }

            public void RemoveChatById(int Id)
            {
                lock (_lock)
                {
                    _activeChats.RemoveAll(c => c.Id == Id);
                }
            }

            public bool ContainsChatById(int id)
            {
                return _activeChats.Exists(c => c.Id == id);
            }

            public Chat? Find(Predicate<Chat> match)
            {
                return _activeChats.Find(match);

            }
        }

        public ChatHub(DBContext dbContext) : base()
        {
            this._dbContext = dbContext;
            this._dbContext.ChangeTracker.LazyLoadingEnabled = false;
        }

        public override async Task OnConnectedAsync()
        {
            try
            {
                string? userEmail = Context.User?.Identity?.Name;
                string? role = Context.User?.FindFirst(ClaimTypes.Role)?.Value;

                if (!string.IsNullOrEmpty(userEmail) && !string.IsNullOrEmpty(role))
                {
                    _usersOnline.TryAdd(userEmail, role);
                }

                while (role == "Admin" && !_serviceQueue.IsEmpty)
                {
                    await ProcessAdminServiceAsync(userEmail);
                }

                if (_dbContext.Chats != null)
                {
                    var userChats = _dbContext.Chats?
                        .Include(chat => chat.Messages)
                        .Include(chat => chat.InterlocutorsEmails)
                        .Where(chat => chat.InterlocutorsEmails.Any(einfo => einfo.Email == userEmail))
                        .ToList();

                    if (!userChats.IsNullOrEmpty())
                    {
                        foreach (var chat in userChats)
                        {
                            if (!_activeChats.ContainsChatById(chat.Id))
                            {
                                _activeChats.AddChat(chat);
                            }
                        }
                    }
                }

                await base.OnConnectedAsync();
            }
            catch (Exception ex)
            {
                string mess = ex.Message;
            }
        }

        private async Task ProcessAdminServiceAsync(string userEmail)
        {
            if (_dbContext.Users != null)
            {
                var admin = await _dbContext.Users
                    .Include(user => user.Role)
                    .Include(a => a.Chats)
                    .ThenInclude(chat => chat.Messages)
                    .FirstOrDefaultAsync(a => a.Email == userEmail);

                if (_serviceQueue.TryDequeue(out var value))
                {
                    var (client, senderName, message) = value;

                    if (client != null && admin != null)
                    {
                        EmailInfo? clientEmail = await _dbContext.Emails.FindAsync(client.Email);
                        if (clientEmail is null)
                        {
                            clientEmail = new EmailInfo() { Email = client.Email };
                            _dbContext.Emails.Add(clientEmail);
                            _dbContext.SaveChanges();
                        }

                        EmailInfo? adminEmail = await _dbContext.Emails.FindAsync(admin.Email);
                        if (adminEmail is null)
                        {
                            adminEmail = new EmailInfo() { Email = admin.Email };
                            _dbContext.Emails.Add(adminEmail);
                            _dbContext.SaveChanges();
                        }

                        var chat = new Chat()
                        {
                            InterlocutorsEmails = new() {
                                clientEmail,
                                adminEmail
                            },
                        };
                        var messageObj = new Message()
                        {
                            SenderEmail = client.Email,
                            SenderName = senderName,
                            Content = message
                        };
                        chat.Messages.Add(messageObj);

                        _dbContext.Chats.Add(chat);
                        await _dbContext.SaveChangesAsync();

                        try
                        {
                            chat = await _dbContext.Chats
                                .Include(c => c.Messages)
                                .Include(c => c.InterlocutorsEmails)
                                .FirstOrDefaultAsync(c => c.InterlocutorsEmails.Any(e => e.Email == admin.Email)
                                    && c.InterlocutorsEmails.Any(e => e.Email == client.Email));

                            if (chat != null)
                            {
                                using (IDbContextTransaction transaction = _dbContext.Database.BeginTransaction())
                                {
                                    try
                                    {
                                        admin.Chats.AddChat(chat, admin.Role);
                                        client.Chats.AddChat(chat, client.Role);

                                        await _dbContext.SaveChangesAsync();

                                        transaction.Commit();
                                    }
                                    catch
                                    {
                                        transaction.Rollback();
                                    }
                                }

                                _activeChats.AddChat(chat);
                                await Clients.User(client.Email).SendAsync("ReceiveMessage", chat.Id, messageObj);
                                await Clients.User(admin.Email).SendAsync("ReceiveMessage", chat.Id, messageObj);
                            }
                        }
                        catch (Exception ex)
                        {
                            string mess = ex.Message;
                        }
                    }
                }
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userEmail = Context.User?.Identity?.Name;
            if (!string.IsNullOrEmpty(userEmail))
            {
                _usersOnline.TryRemove(userEmail, out var values);
            }

            var userChats = await _dbContext.Chats?
                .Include(chat => chat.InterlocutorsEmails)
                .Include(chat => chat.Messages)
                .Where(chat => chat.InterlocutorsEmails.Any(einfo => einfo.Email == userEmail))
                .AsSplitQuery()
                .ToListAsync();

            if (userChats != null && userEmail != null)
            {
                ProcessChatsToRemoveAsync(userChats, userEmail);
            }

            await base.OnDisconnectedAsync(exception);
        }

        private void ProcessChatsToRemoveAsync(List<Chat> userChats, string userEmail)
        {
            var chatsIdToRemove = userChats
                .Where(userChat => _activeChats.ContainsChatById(userChat.Id) &&
                    !userChat.InterlocutorsEmails.Where(einfo => einfo.Email != userEmail)
                        .Any(einfo => _usersOnline.ContainsKey(einfo.Email)))
                .Select(chat => chat.Id)
                .ToList();

            foreach (var chatIdToRemove in chatsIdToRemove)
            {
                _activeChats.RemoveChatById(chatIdToRemove);
            }
        }

        public async Task Send(string senderName, string message, int chatId)
        {
            if (chatId <= 0)
            {
                User? user = _dbContext.Users
                    .Include(user => user.Role)
                    .Include(user => user.Chats)
                    .ThenInclude(chat => chat.Messages)
                    .FirstOrDefault(user => user.Email == Context.User.Identity.Name);
                
                if (user != null)
                {
                    foreach (var tuple in _serviceQueue)
                    {
                        if (tuple.Item1.Email == user.Email)
                        {
                            message = "You are already in the queue for service";
                            await Clients.User(user.Email).SendAsync("ReceiveMessage", chatId, new Message()
                            {
                                Content = message,
                                SenderEmail = string.Empty,
                                SenderName = "System",
                            });
                            return;
                        }
                    }
                    _serviceQueue.Enqueue((user, senderName, message));

                    if (_usersOnline.Any(user => user.Value == "Admin"))
                    {
                        var adminsId = _usersOnline.Where(user => user.Value == "Admin").Select(user => user.Key).ToList();
                        if (adminsId.Any())
                        {
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
                        message = "Unfortunately, we don't have any online administrators at the moment. " +
                            "Your message has been queued and will be processed when an administrator is connected.";
                        await Clients.User(user.Email).SendAsync("ReceiveMessage", chatId, new Message()
                        {
                            Content = message,
                            SenderEmail = string.Empty,
                            SenderName = "System",
                        });
                    }
                }
            }
            else
            {
                var chat = _activeChats.Find(chat => chat.Id == chatId);

                if (chat != null)
                {
                    var messageObj = new Message()
                    {
                        SenderEmail = Context.User.Identity.Name,
                        SenderName = senderName,
                        Content = message,
                    };

                    try
                    {
                        _dbContext.Attach(chat);
                        chat.Messages.Add(messageObj);

                        await _dbContext.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        string mess = ex.Message;
                    }
                    foreach (var userEmail in chat.InterlocutorsEmails.Where(e => e.Email != messageObj.SenderEmail))
                    {
                        await Clients.User(userEmail.Email).SendAsync("ReceiveMessage", chat.Id, messageObj);
                    }
                }
            }
        }

        public async Task ReadSet(string userEmail, int chatId)
        {
            var chat = _activeChats.Find(c => c.Id == chatId);
            if (chat != null)
            {
                _dbContext.Attach(chat);
                chat.Messages.Where(m => !m.IsRead && m.SenderEmail != userEmail).ToList().ForEach(m => m.IsRead = true);
                await _dbContext.SaveChangesAsync();
            }
        }
    }
}
