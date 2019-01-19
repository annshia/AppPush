using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Syscom.App.Push.API.Models
{
    public partial class Tokens
    {
        public enum Statuses : byte { Disable = 0, Enable }

        public int Id { get; set; }
        public string Token { get; set; }
        public string Account { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime TimeOutAt { get; set; }
    }
}
