using System.Security.Claims;
using ChatApp.server.Class;
using ChatApp.server.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ChatApp.server.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly ChatContext _context;
        private readonly ConnectedUsersService _connectedUsers;

        public ChatHub(ChatContext context, ConnectedUsersService connectedUsers)
        {
            _context = context;
            _connectedUsers = connectedUsers;
        }
        /// <summary>
        /// Handles new client connections to the chat hub. 
        /// When a client connects, it retrieves the username from the JWT token claims and attempts to add the user to the list of active users.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="HubException">thrown when the username is already taken</exception>
        public override async Task OnConnectedAsync()
        {
            var username = Context.User?.FindFirst(ClaimTypes.Name)?.Value
                        ?? Context.User?.FindFirst("unique_name")?.Value
                        ?? "Unknown";

            if (!_connectedUsers.TryAddUser(username, Context.ConnectionId))
            {
                throw new HubException("Username already taken. Please choose another.");
            }

            await Clients.All.SendAsync("ReceiveMessage", "System", $"{username} has joined the chat.");
            await base.OnConnectedAsync();
        }
        /// <summary>
        /// Handles client disconnections from the chat hub. 
        /// When a client disconnects, it retrieves the username associated with the connection ID and removes the user from the list of active users.
        /// </summary>
        /// <param name="exception">The exception that occurred during disconnection.</param>
        /// <returns></returns>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var username = _connectedUsers.GetUsername(Context.ConnectionId);
            if (username != null)
            {
                _connectedUsers.RemoveUser(Context.ConnectionId);
                await Clients.All.SendAsync("ReceiveMessage", "System", $"{username} has left the chat.");
            }
            await base.OnDisconnectedAsync(exception);
        }
        /// <summary>
        /// Handles incoming chat messages from clients. 
        /// When a client sends a message, it retrieves the username and user ID from the JWT token claims, sanitizes the input message, and saves it to the database. 
        /// The message is then broadcast to all connected clients with the sender's username.
        /// </summary>
        /// <param name="message">The chat message to send.</param>
        /// <returns></returns>
        public async Task SendMessage(string message)
        {
            var username = Context.User?.FindFirst(ClaimTypes.Name)?.Value
                        ?? Context.User?.FindFirst("unique_name")?.Value
                        ?? "Unknown";
            var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int.TryParse(userIdClaim, out var userId);

            // Sanitize input
            message = message?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(message)) return;
            if (message.Length > 2000) message = message[..2000];

            // Strip control characters (except common whitespace)
            message = new string(message.Where(c => !char.IsControl(c) || c == '\n' || c == '\r' || c == '\t').ToArray());

            // Persist to database
            var chatMessage = new ChatMessage
            {
                Content = message,
                Created = DateTime.UtcNow,
                SenderId = userId
            };
            _context.ChatMessages.Add(chatMessage);
            await _context.SaveChangesAsync();

            await Clients.All.SendAsync("ReceiveMessage", username, message);
        }
    }
}
