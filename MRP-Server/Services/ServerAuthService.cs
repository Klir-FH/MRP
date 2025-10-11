using MRP_DAL;
using MRP_DAL.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MRP_Server.Services
{
    public class ServerAuthService
    {
        private readonly AuthService _authService;
        private readonly TokenManager _tokenManager;
        private readonly UserRepository _userRepository;
        public int? TokenUserId => _tokenManager.UserId;

        public ServerAuthService(AuthService authService, UserRepository userRepository, TokenManager tokenManager)
        {
            _authService = authService;
            _userRepository = userRepository;
            _tokenManager = tokenManager;
        }

        public async Task<string?> TryLoginAsync(string username, string password)
        {
            var valid = await _authService.VerifyCredentialsAsync(username, password);
            if (!valid) return null;

            var userId = await _userRepository.GetUserIdByUsernameAsync(username);
            if (userId == null) return null;

            return _tokenManager.GenerateJwtToken(username, userId.Value);
        }
        public async Task<bool> RegisterAsync(string username, string password)
        {
            return await _authService.RegisterAsync(username, password);
        }
        public bool ValidateToken(string token) => _tokenManager.ValidateToken(token);

        public string? GetTokenSubject(string token)
        {
            if (!_tokenManager.ValidateToken(token)) return null;

            return _tokenManager.Subject;
        }
    }
}
