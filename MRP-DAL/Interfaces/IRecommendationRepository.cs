using Models.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MRP_DAL.Interfaces
{
    public interface IRecommendationRepository
    {
        Task<List<MediaEntryDTO>> GetRecommendationsAsync(int userId, string type, int limit = 20);
    }
}
