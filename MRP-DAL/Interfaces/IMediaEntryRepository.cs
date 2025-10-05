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
        Task<int> CreateAsync(MediaEntry media);
        Task<List<MediaEntry>> GetAllAsync();
        Task<bool> DeleteAsync(int id, int ownerId);
    }
}
