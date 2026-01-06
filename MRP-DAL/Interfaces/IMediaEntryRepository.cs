using Models.DTOs;
using MRP.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MRP_DAL.Interfaces
{
    public interface IMediaEntryRepository
    {
        Task<int> CreateAsync(MediaEntryDTO media);
        Task<bool> DeleteAsync(int id, int ownerId);
        Task<List<MediaEntryDTO>> SearchAsync(string? query, string? genre, int? type,
            string? year, int? ageRestriction, double? minScore, string sortBy, string sortOrder, int? userId);
        Task<int?> GetOwnerIdByMediaIdAsync(int mediaId);
        Task<List<string>> GetGenresAsync(int mediaId);
        Task SetGenresAsync(int mediaId, List<string> genres);
        Task<bool> UpdateAsync(MediaEntryDTO media);
 
        Task<MediaEntryDTO?> GetByIdAsync(int mediaId, int? userId);

        //could be extracted into own interface but the yaml makes it seem that it should be in here
        Task<List<int>> GetFavoriteMediaIdsAsync(int userId);
        Task<bool> FavouriteAsync(int userId, int mediaId);
        Task<bool> UnfavouriteAsync(int userId, int mediaId);


    }
}
