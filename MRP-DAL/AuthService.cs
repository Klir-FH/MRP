using MRP_DAL.Interfaces;
using MRP_DAL.Repositories;
using System.Threading.Tasks;

namespace MRP_DAL
{
    public class AuthService
    {
        private readonly CredentialsRepository _credentialsRepository;
        private readonly UserRepository _userRepository;

        public AuthService(CredentialsRepository credentialsRepository, UserRepository userRepository)
        {
            _userRepository = userRepository;
            _credentialsRepository = credentialsRepository;
        }

        public async Task<bool> VerifyCredentialsAsync(string username, string password)
        {
            var storedHash = await _credentialsRepository.GetHashedPasswordByUsernameAsync(username);
            return storedHash != null && BCrypt.Net.BCrypt.Verify(password, storedHash);
        }

        public async Task<bool> RegisterAsync(string username, string password)
        {

            var existing = await _userRepository.GetUserIdByUsernameAsync(username);
            if (existing != null) return false;

            int userId = await _userRepository.CreateUserAsync(username);

            var hashed = BCrypt.Net.BCrypt.HashPassword(password);

            return await _credentialsRepository.InsertCredentialsAsync(userId, hashed, username);
        }
    }
}
