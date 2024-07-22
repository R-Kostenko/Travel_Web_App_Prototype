using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.IdentityModel.Tokens;
using Models;
using System.Collections.Concurrent;
using System.Security.Claims;
using Travel_App_Web.Data;

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
            private readonly List<Chat> _activeChats = new();
            private readonly object _lock = new();

            public void AddChat(Chat chat)
            {
                lock (_lock)
                {
                    _activeChats.Add(chat);
                }
            }

            public void RemoveChatById(long Id)
            {
                lock (_lock)
                {
                    _activeChats.RemoveAll(c => c.ChatId == Id);
                }
            }

            public bool ContainsChatById(long id) => _activeChats.Exists(c => c.ChatId == id);

            public Chat? Find(Predicate<Chat> match) => _activeChats.Find(match);
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
                        .Where(chat => chat.Emails.Contains(userEmail))
                        .ToList();

                    if (!userChats.IsNullOrEmpty())
                    {
                        foreach (var chat in userChats)
                        {
                            if (!_activeChats.ContainsChatById(chat.ChatId))
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
                    .Include(user => user.Chats)
                    .ThenInclude(chat => chat.Messages)
                    .AsSplitQuery()
                    .FirstOrDefaultAsync(user => user.Email == userEmail);

                if (_serviceQueue.TryDequeue(out var value))
                {
                    var (client, senderName, message) = value;

                    if (client != null && admin != null)
                    {
                        var chat = new Chat()
                        {
                            InterlocutorsEmails = new() {
                                client.Email,
                                admin.Email
                            }
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
                                .FirstOrDefaultAsync(c => c.Emails.Contains(admin.Email) && c.Emails.Contains(client.Email));

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

                                List<Task> sendTasks = new()
                                {
                                    Clients.User(client.Email).SendAsync("ReceiveMessage", chat.ChatId, messageObj),
                                    Clients.User(admin.Email).SendAsync("ReceiveMessage", chat.ChatId, messageObj)
                                };
                                await Task.WhenAll(sendTasks);
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
                .Include(chat => chat.Messages)
                .Where(chat => chat.Emails.Contains(userEmail))
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
                .Where(userChat => _activeChats.ContainsChatById(userChat.ChatId) &&
                    !userChat.InterlocutorsEmails.Where(e => e != userEmail)
                        .Any(e => _usersOnline.ContainsKey(e)))
                .Select(chat => chat.ChatId)
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
                    .AsSplitQuery()
                    .FirstOrDefault(user => user.Email == Context.User.Identity.Name);
                
                if (user != null)
                {
                    foreach (var tuple in _serviceQueue)
                    {
                        if (tuple.Item1.Email == user.Email)
                        {
                            message = "Your message is already in the queue for service";
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
                                .OrderBy(admin => admin.Chats.Count)
                                .AsSplitQuery()
                                .FirstAsync();

                            await ProcessAdminServiceAsync(admin.Email);
                        }
                    }
                    else
                    {
                        message = "Unfortunately, we do not have online administrators at the moment. " +
                            "Your message has been queued and will be processed when the administrator connects.";
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
                var chat = _activeChats.Find(chat => chat.ChatId == chatId);

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

                    List<Task> sendTasks = new();
                    foreach (var userEmail in chat.InterlocutorsEmails.Where(e => e != messageObj.SenderEmail))
                    {
                        sendTasks.Add(Clients.User(userEmail).SendAsync("ReceiveMessage", chat.ChatId, messageObj));
                    }
                    await Task.WhenAll(sendTasks);
                }
            }
        }

        public async Task ReadSet(string userEmail, int chatId)
        {
            var chat = _activeChats.Find(c => c.ChatId == chatId);
            if (chat != null)
            {
                _dbContext.Attach(chat);
                chat.Messages.Where(m => !m.IsRead && m.SenderEmail != userEmail).ToList().ForEach(m => m.IsRead = true);
                await _dbContext.SaveChangesAsync();
            }
        }
    }
}
