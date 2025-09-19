using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MRP.Models
{
    public class MediaEntry
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string ReleaseYear { get; set; }
        public List<string> Genres { get; set; } = new();
        public int AgeRestriction { get; set; }

        public int? Owner { get; set; } = null;
        public List<int> RatingValues { get; set; } = new();
        public List<int> LikedBy { get; set; } = new();
        public MediaType Type { get; set; }
        //may be obsulent
        public List<int> FavouritedBy { get; set; } = new();

        public double? RatingAverage { get; set; } = null;
	}
}
