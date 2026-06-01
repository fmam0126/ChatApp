using Microsoft.EntityFrameworkCore;
using ChatApp.server.Models;
namespace ChatApp.server.Interfaces
{
    public interface IChatContext
    {
        DbSet<ChatMessage> ChatMessages { get; set; }
        DbSet<User> Users { get; set; }
        //Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
