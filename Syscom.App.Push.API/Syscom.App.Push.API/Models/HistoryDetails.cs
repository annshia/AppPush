using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Syscom.App.Push.API.Models
{
    public partial class HistoryDetails
    {
        public enum Statuses : byte { Failed = 0, Waiting, Succeed }

        public int Id { get; set; }
        public int? SubscriberId { get; set; }
        public string Message { get; set; }
        public DateTime CreatedAt { get; set; }
        public int ChannelId { get; set; }
        public byte? Status { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? FinishedAt { get; set; }
        public int HistoriesId { get; set; }


        [ForeignKey("ChannelId")]
        public Channels Channel { get; set; }

        [ForeignKey("SubscriberId")]
        public Subscribers Subscriber { get; set; }

    }
}
