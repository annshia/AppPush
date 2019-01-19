using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Syscom.App.Push.API.Models
{
    public partial class ApiChannels
    {
        public int ApiId { get; set; }
        public int ChannelId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        [ForeignKey("ApiId")]
        public Apis Api { get; set; }

        [ForeignKey("ChannelId")]
        public Channels Channel { get; set; }
    }
}
