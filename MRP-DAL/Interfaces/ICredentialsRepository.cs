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
        Credentials? GetByUsername(string username);
        Task<bool> SetCredentials(string username, string password, int userId);
        Task<bool> TryLogin(string username, string hashedPassword);
    }
}
