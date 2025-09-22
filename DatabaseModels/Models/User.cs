using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MRP.Models
{
    public class User
    {
        public int Id { get; set; }

        public string Username { get; set; }
        public List<int> RatingHistory { get; set; } = new();
        public List<int> FavouriteMediaEntries { get; set; } = new();
    }
}
