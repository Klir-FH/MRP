using Models;
using Models.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MRP_DAL.Interfaces
{
    public interface IUserRepository
    {
        Task<int?> GetUserIdByUsernameAsync(string username);
        Task<int> CreateUserAsync(string username);
        Task<UserProfileStatisticsDTO?> GetUserProfileStatsAsync(string username, CancellationToken ct = default);
        Task<(string? Email, Genre? FavoriteGenre)?> GetProfileAsync(int userId);
        Task<bool> UpdateProfileAsync(int userId, string? email, string favoriteGenre);
        Task<string?> GetUsernameByIdAsync(int userId);
        Task<object> GetLeaderboardAsync(int limit);
    }
}
