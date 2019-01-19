using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Syscom.App.Push.API.Models
{
    public partial class Subscribers
    {
        public enum Types : byte { IOS = 1, Android, Line }

        public int? ChannelId { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string DeviceToken { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public byte? Status { get; set; }
        public byte? Type { get; set; }
        public int Id { get; set; }

        [ForeignKey("ChannelId")]
        public Channels Channel { get; set; }
    }
}
