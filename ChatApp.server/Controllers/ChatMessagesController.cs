using System.Security.Claims;
using ChatApp.server.Models;
using ChatApp.server.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;

namespace ChatApp.server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ChatMessagesController : ControllerBase
    {
        private readonly ChatContext _context;
        public ChatMessagesController(ChatContext context)
        {
            _context = context;
        }

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
                    SenderName = m.Sender.Name
                })
                .ToListAsync();

            // Return in chronological order (oldest first) for display
            chatMessages.Reverse();
            return Ok(chatMessages);
        }

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
            await _context.Entry(chatMessage).Reference(m => m.Sender).LoadAsync();

            return Ok(new ChatMessageResponseDTO
            {
                Id = chatMessage.Id,
                Content = chatMessage.Content,
                Created = chatMessage.Created,
                SenderId = chatMessage.SenderId,
                SenderName = chatMessage.Sender.Name
            });
        }
    }
}

