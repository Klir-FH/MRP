using MRP_DAL.Repositories;
using System.Threading.Tasks;

namespace MRP_DAL{
  public class AuthService{
      private readonly CredentialsRepository _credentialsRepository; 

      public AuthService(CredentialsRepository credentialsRepository){
        _credentialsRepository = credentialsRepository;
      }

      public async Task<bool> TryLoginAsync(string username, string password){
        var storedHash = await _credentialsRepository.GetHashedPasswordByUsernameAsync(username);
        if(storedHash == null) return false;

        return BCrypt.Net.BCrypt.Verify(password,storedHash);
      }

      public async Task<bool> RegisterAsync(string username, string password, int userId){
        var hashed = BCrypt.Net.BCrypt.HashPassword(password);
        return await _credentialsRepository.InsertCredentialsAsync(username,hashed, userId);

      }
  }
}
