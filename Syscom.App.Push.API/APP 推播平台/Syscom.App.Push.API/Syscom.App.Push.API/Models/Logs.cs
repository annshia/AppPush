using System;
using System.Collections.Generic;

namespace Syscom.App.Push.API.Models
{
    public partial class Logs
    {
        public int Id { get; set; }
        public byte Type { get; set; }
        public int? Acnt { get; set; }
        public int? Role { get; set; }
        public string ServerName { get; set; }
        public string ServerIp { get; set; }
        public string Action { get; set; }
        public byte Result { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
