using MRP.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.DTOs
{
    public class MediaEntryDTO
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string? Description { get; set; }
        public string? ReleaseYear { get; set; }
        public int? AgeRestriction { get; set; }
        public int? OwnerId { get; set; } = null;
        public MediaType Type { get; set; }

        public List<string>? Genres { get; set; }
        public double? AvgScore { get; set; }
        public int RatingCount { get; set; }
        public bool IsFavorited { get; set; }
        public bool IsLiked { get; set; }
    }
}
