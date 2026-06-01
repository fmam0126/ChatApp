using ChatApp.CliClient.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace ChatApp.CliClient.Interfaces
{
    internal interface IChatClient
    {
        public Task<bool> SendMessageAsync(string message);
        public Task<IEnumerable<ChatMessage>> GetMessagesAsync();
    }
}
