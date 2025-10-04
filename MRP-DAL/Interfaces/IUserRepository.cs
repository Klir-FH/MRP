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
    }
}
