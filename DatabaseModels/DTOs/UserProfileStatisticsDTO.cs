using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.DTOs
{
    public class UserProfileStatisticsDTO
    {
        public string Username { get; set; } = "";
        public int TotalRatings { get; set; }
        public double AverageScore { get; set; }
        public string? FavoriteGenre { get; set; }
        public int FavoritesCount { get; set; }
    }
}
