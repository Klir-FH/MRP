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
        public string Title { get; set; }
        public string? Description { get; set; }
        public string? ReleaseYear { get; set; }
        public int? AgeRestriction { get; set; }
        public MediaType Type { get; set; }
    }
}
