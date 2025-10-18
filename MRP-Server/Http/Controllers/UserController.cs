using Models.DTOs;
using MRP_DAL.Repositories;
using MRP_Server.Http.Helpers;
using MRP_Server.Services;
using System.Net;

namespace MRP_Server.Http.Controllers
{
    public class UserController
    {
        private readonly ServerAuthService _serverAuthService;
        private readonly UserRepository _userRepository;

        public UserController(ServerAuthService authService, UserRepository userRepository)
        {
            _serverAuthService = authService;
            _userRepository = userRepository;
        }

        public async Task HandleAsync(HttpListenerContext listenerContext)
        {
            var httpRequest = listenerContext.Request;
            var httpResponse = listenerContext.Response;

            try
            {
                var path = httpRequest.Url.AbsolutePath;
                var method = httpRequest.HttpMethod;

                if (method == "POST" && path == "/api/users/register")
                    await RegisterAsync(httpRequest, httpResponse);
                else if (method == "POST" && path == "/api/users/login")
                    await LoginAsync(httpRequest, httpResponse);
                else if (method == "GET" && path.EndsWith("/profile"))
                    await HandleUserProfileRequest(httpRequest, httpResponse);
                else
                {
                    httpResponse.StatusCode = 404;
                    await JsonSerializationHelper.WriteJsonAsync(httpResponse, new { error = "Endpoint not found" });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UserController error: {ex}");
                httpResponse.StatusCode = 500;
                await JsonSerializationHelper.WriteJsonAsync(httpResponse, new { error = "Internal Server Error" });
            }
            finally
            {
                httpResponse.Close();
            }
        }

        private async Task RegisterAsync(HttpListenerRequest httpRequest, HttpListenerResponse httpResponse)
        {
            var creds = await JsonSerializationHelper.ReadJsonAsync<LoginDTO>(httpRequest);

            if (creds == null || string.IsNullOrWhiteSpace(creds.Username) || string.IsNullOrWhiteSpace(creds.Password))
            {
                httpResponse.StatusCode = 400;
                await JsonSerializationHelper.WriteJsonAsync(httpResponse, new { error = "Invalid JSON body" });
                return;
            }

            bool created = await _serverAuthService.RegisterAsync(creds.Username, creds.Password);

            httpResponse.StatusCode = created ? 201 : 400;
            await JsonSerializationHelper.WriteJsonAsync(httpResponse, new { success = created });
        }

        private async Task LoginAsync(HttpListenerRequest httpRequest, HttpListenerResponse httpResponse)
        {
            var creds = await JsonSerializationHelper.ReadJsonAsync<LoginDTO>(httpRequest);

            if (creds == null || string.IsNullOrWhiteSpace(creds.Username) || string.IsNullOrWhiteSpace(creds.Password))
            {
                httpResponse.StatusCode = 400;
                await JsonSerializationHelper.WriteJsonAsync(httpResponse, new { error = "Invalid request body" });
                return;
            }

            var token = await _serverAuthService.TryLoginAsync(creds.Username, creds.Password);

            if (token == null)
            {
                httpResponse.StatusCode = 401;
                await JsonSerializationHelper.WriteJsonAsync(httpResponse, new { error = "Invalid credentials" });
                return;
            }

            httpResponse.StatusCode = 200;
            await JsonSerializationHelper.WriteJsonAsync(httpResponse, new { token });
        }

        private async Task HandleUserProfileRequest(HttpListenerRequest httpRequest, HttpListenerResponse httpResponse)
        {

            var (isValid, tokenUsername) = AuthHelper.ValidateAndExtractToken(httpRequest, httpResponse,_serverAuthService);
            if (!isValid || string.IsNullOrWhiteSpace(tokenUsername))
                return;

            string username = httpRequest.Url?.Segments.Reverse().Skip(1).FirstOrDefault()?.Trim('/') ?? "";

            var stats = await _userRepository.GetUserProfileStatsAsync(username);

            // can only see their own profile
            // TODO: Visibility Settings
            if (!string.Equals(username, tokenUsername, StringComparison.OrdinalIgnoreCase))
            {
                httpResponse.StatusCode = 403;
                await JsonSerializationHelper.WriteJsonAsync(httpResponse, new { error = "Access denied" });
                return;
            }

            if (stats == null)
            {
                httpResponse.StatusCode = 404;
                await JsonSerializationHelper.WriteJsonAsync(httpResponse, new { error = "User not found" });
                return;
            }

            httpResponse.StatusCode = 200;
            await JsonSerializationHelper.WriteJsonAsync(httpResponse, stats);
        }

    }
}
