using System.ComponentModel.DataAnnotations;

namespace ChatApp.server.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Name { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;

    }
}
