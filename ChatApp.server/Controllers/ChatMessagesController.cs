using System.Security.Claims;
using ChatApp.server.Models;
using ChatApp.server.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;

namespace ChatApp.server.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class ChatMessagesController : ControllerBase
    {
        private readonly ChatContext _context;
        public ChatMessagesController(ChatContext context)
        {
            _context = context;
        }
        /// <summary>
        /// Handles GET requests to retrieve chat messages. The endpoint is protected by authorization, requiring a valid JWT token. 
        /// It retrieves the most recent chat messages from the database, including the sender's name, and returns them in chronological order. 
        /// </summary>
        /// <returns>The list of chat messages.</returns>
        [HttpGet(Name = "GetChats")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<ChatMessageResponseDTO>>> Get()
        {
            int count = 50; // Default number of messages to return
            count = Math.Clamp(count, 1, 200);
            var chatMessages = await _context.ChatMessages
                .Include(m => m.Sender)
                .OrderByDescending(m => m.Created)
                .Take(count)
                .Select(m => new ChatMessageResponseDTO
                {
                    Id = m.Id,
                    Content = m.Content,
                    Created = m.Created,
                    SenderId = m.SenderId,
                    SenderName = m.Sender != null ? m.Sender.Name : "Unknown"
                })
                .ToListAsync();

            // Return in chronological order (oldest first) for display
            chatMessages.Reverse();
            return Ok(chatMessages);
        }

        /// <summary>
        /// Handles POST requests to create new chat messages. The endpoint is protected by authorization, requiring a valid JWT token. 
        /// It associates the new chat message with the authenticated user as the sender and saves it to the database.
        /// Currently unused as chat messages are created through SignalR.
        /// </summary>
        /// <param name="newChatMessage">The DTO containing the details of the new chat message.</param>
        /// <returns>The created chat message.</returns>
        [HttpPost(Name = "PostChat")]
        [Authorize]
        public async Task<ActionResult<ChatMessageResponseDTO>> Post(ChatMessageDTO newChatMessage)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim is null || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            var chatMessage = new ChatMessage
            {
                Content = newChatMessage.Content,
                Created = DateTime.UtcNow,
                SenderId = userId
            };
            _context.ChatMessages.Add(chatMessage);
            await _context.SaveChangesAsync();

            // Load the sender name for the response
            var sender = await _context.Users.FindAsync(chatMessage.SenderId);

            return Ok(new ChatMessageResponseDTO
            {
                Id = chatMessage.Id,
                Content = chatMessage.Content,
                Created = chatMessage.Created,
                SenderId = chatMessage.SenderId,
                SenderName = sender?.Name ?? "Unknown"
            });
        }
    }
}

