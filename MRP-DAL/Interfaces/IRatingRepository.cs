using MRP.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MRP_DAL.Interfaces
{
    public interface IRatingRepository
    {
        Task<int> CreateAsync(Rating rating);
        Task<List<Rating>> GetByMediaAsync(int mediaEntryId);
        Task<bool> ConfirmCommentAsync(int ratingId, int ownerId);
        Task<bool> LikeRatingAsync(int ratingId, int userId);
        Task<bool> UnlikeRatingAsync(int ratingId, int userId);
        Task<int> GetLikeCountAsync(int ratingId);
    }
}
