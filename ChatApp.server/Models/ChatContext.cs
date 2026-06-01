using ChatApp.server.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.server.Models

{
    public class ChatContext(DbContextOptions<ChatContext> options) : DbContext(options), IChatContext
    {
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<User> Users { get; set; }
    }
}
