using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Syscom.App.Push.API.Models
{
    public partial class Apis
    {
        public enum Statuses : byte { Disable = 0, Enable }

        public int Id { get; set; }
        public string Name { get; set; }
        public Guid Uuid { get; set; }
        public string SecretKey { get; set; }
        public string Webhook { get; set; }
        public byte Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        [ForeignKey("ApiId")]
        public List<Histories> Histories { get; set; }

        [NotMapped]
        public int PushCount { get; set; }
    }
}
