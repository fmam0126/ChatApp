using ChatApp.server.Models;
using ChatApp.server.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace ChatApp.server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ChatMessagesController : ControllerBase
    {
        private readonly ChatContext _context;
        public ChatMessagesController(ChatContext context) {
            _context = context;
        }

        [HttpGet(Name = "GetChats")]
        public async Task<ActionResult<IEnumerable<ChatMessages>>> Get()
        {
            // Todo Add pagination
            var chatMessages = await _context.ChatMessages.ToListAsync();
            return Ok(chatMessages);
        }

        [HttpPost(Name = "PostChat")]
        public async Task<ActionResult<ChatMessages>> Post(ChatMessageDTO newChatMessage)
        {
            // Implementation for posting a new chat message
            var chatMessage = new ChatMessages
            {
                Content = newChatMessage.Content,
                Created = DateTime.UtcNow,
                SenderId = 1 // Assuming a default sender ID for demonstration
            };
            _context.ChatMessages.Add(chatMessage);
            await _context.SaveChangesAsync();

            return Ok(chatMessage);
        }
    }
}

