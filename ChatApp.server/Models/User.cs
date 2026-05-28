using System.ComponentModel.DataAnnotations;

namespace ChatApp.server.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Name { get; set; } = string.Empty;

    }
}
