using System;
using System.Collections.Generic;
using System.Text;

namespace ChatApp.CliClient.Models
{
    internal class ChatMessage
    {
        public int Id { get; set; }
        public string content { get; set; } = string.Empty;
        public DateTime Created { get; set; }
        public int senderId { get; set; }
    }
}
