using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MRP.Models
{
    public class Rating
    {
      public int Id { get; set; }
      public int MediaEntryId { get; set; }
      public int StarValue { get; set; }
      public string? Comment { get; set; }
      public DateTime? Timestamp { get; set; }
      public bool IsCommentVisible { get; set; } = false;

      public int OwnerId { get; set; }
    }
}
