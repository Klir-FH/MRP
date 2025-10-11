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
        Task<List<MediaEntryDTO>> GetAllAsync();
        Task<bool> DeleteAsync(int id, int ownerId);
    }
}
