using MRP_Server.Services;
using MRP_Server.Http.Helpers;
using System.Net;
using Models.DTOs;

namespace MRP_Server.Http.Controllers
{
    public class UserController
    {
        private readonly ServerAuthService _serverAuthService;

        public UserController(ServerAuthService authService)
        {
            _serverAuthService = authService;
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
                else if (method == "GET" && path.StartsWith("/api/users/"))
                    await ProfileAsync(httpRequest, httpResponse);
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

        private async Task ProfileAsync(HttpListenerRequest req, HttpListenerResponse res)
        {
            var validation = ValidateAndExtractToken(req, res);
            if (!validation.IsValid)
                return;

            var username = validation.Username ?? "unknown";

            // TODO: replace with actuall response

            res.StatusCode = 200;
            await JsonSerializationHelper.WriteJsonAsync(res, new UserProfileDTO
            {
                Username = username,
                Message = $"Welcome to your profile, {username}!"
            });
        }

        private (bool IsValid, string? Username) ValidateAndExtractToken(HttpListenerRequest req, HttpListenerResponse res)
        {
            string? header = req.Headers["Authorization"];
            if (header == null || !header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                res.StatusCode = 401;
                JsonSerializationHelper.WriteJsonAsync(res, new { error = "Missing or invalid Authorization header" }).Wait();
                return (false, null);
            }

            string token = header.Substring("Bearer ".Length);

            if (!_serverAuthService.ValidateToken(token))
            {
                res.StatusCode = 403;
                JsonSerializationHelper.WriteJsonAsync(res, new { error = "Invalid or expired token" }).Wait();
                return (false, null);
            }

            string? username = _serverAuthService.GetTokenSubject(token);
            return (true, username);
        }
    }
}
