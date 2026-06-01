using System;
using System.Collections.Generic;
using System.Text;

namespace ChatApp.CliClient.Models
{
    internal class Settings
    {
        public required string ServerUrl { get; set; }
        public required string UserName { get; set; }
        public required string AcessToken { get; set; }
    }
}
