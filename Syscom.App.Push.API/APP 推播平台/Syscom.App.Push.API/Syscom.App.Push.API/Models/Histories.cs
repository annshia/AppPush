using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Syscom.App.Push.API.Models
{
    public partial class Histories
    {
        public int Id { get; set; }
        public int? ApiId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? FinishedAt { get; set; }
        public int? Total { get; set; }
        public int? Success { get; set; }
        public int? Fail { get; set; }
        public string Name { get; set; }


        [ForeignKey("HistoriesId")]
        public List<HistoryDetails> HistoryDetails { get; set; }
    }
}
