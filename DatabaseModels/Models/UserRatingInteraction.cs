using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Models
{
    public class UserRatingInteraction
    {
        public int UserId { get; set; }
        public int RatingId { get; set; }
        public UserMediaInteractions InteractionType { get; set; } = UserMediaInteractions.Like;
    }
}
