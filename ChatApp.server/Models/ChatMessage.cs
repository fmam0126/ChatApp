using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatApp.server.Models
{
    public class ChatMessage
    {
        [Key]
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime Created { get; set; }
        [ForeignKey(nameof(User.Id))]
        public int SenderId { get; set; }
    }
}
