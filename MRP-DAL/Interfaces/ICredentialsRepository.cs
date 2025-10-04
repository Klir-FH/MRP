using MRP.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MRP_DAL.Interfaces
{
    public interface ICredentialsRepository
    {
        Task<string?> GetHashedPasswordByUsernameAsync(string username);
        Task<bool> InsertCredentialsAsync(int userId, string hashedPsw, string username);
    }
}
