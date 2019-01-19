using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Syscom.App.Push.API.Models
{
    public partial class Channels
    {
        public enum Types : byte { Email = 1, SMS, App, Line }
        public enum Statuses : byte { Disable = 0, Enable }


        public int Id { get; set; }
        public string Name { get; set; }
        public byte? Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string Image { get; set; }
        public string ExtraUrlA { get; set; }
        public string ExtraUrlB { get; set; }
        public string ForeignId { get; set; }
        public string SecretKey { get; set; }
        public string ForeignWebhook { get; set; }
        public int? ChannelId { get; set; }
        public byte? Type { get; set; }
        public string ChannelKey { get; set; }

        public List<Subscribers> Subscribers { get; set; }
        [NotMapped]
        public int SubscriberCount { get; set; }
    }
}
