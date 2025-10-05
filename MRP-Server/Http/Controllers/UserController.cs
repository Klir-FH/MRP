using MRP_Server.Services;
using MRP_Server.Http.Helpers;
using System.Net;

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
            var data = await JsonSerializationHelper.ReadJsonAsync<Dictionary<string, string>>(httpRequest);
            if (data == null || !data.ContainsKey("username") || !data.ContainsKey("password"))
            {
                httpResponse.StatusCode = 400;
                await JsonSerializationHelper.WriteJsonAsync(httpResponse, new { error = "Invalid JSON body" });
                return;
            }

            bool created = await _serverAuthService.RegisterAsync(data["username"], data["password"]);
            httpResponse.StatusCode = created ? 201 : 400;
            await JsonSerializationHelper.WriteJsonAsync(httpResponse, new { success = created });
        }

        private async Task LoginAsync(HttpListenerRequest httpRequest, HttpListenerResponse httpResponse)
        {
            var data = await JsonSerializationHelper.ReadJsonAsync<Dictionary<string, string>>(httpRequest);
            if (data == null || !data.ContainsKey("username") || !data.ContainsKey("password"))
            {
                httpResponse.StatusCode = 400;
                await JsonSerializationHelper.WriteJsonAsync(httpResponse, new { error = "Invalid JSON body" });
                return;
            }

            var token = await _serverAuthService.TryLoginAsync(data["username"], data["password"]);
            if (token == null)
            {
                httpResponse.StatusCode = 401;
                await JsonSerializationHelper.WriteJsonAsync(httpResponse, new { error = "Invalid credentials" });
                return;
            }

            httpResponse.StatusCode = 200;
            await JsonSerializationHelper.WriteJsonAsync(httpResponse, new { token });
        }

        private async Task ProfileAsync(HttpListenerRequest httpRequest, HttpListenerResponse httpResponse)
        {
            string? authHeader = httpRequest.Headers["Authorization"];
            if (authHeader == null || !authHeader.StartsWith("Bearer "))
            {
                httpResponse.StatusCode = 401;
                await JsonSerializationHelper.WriteJsonAsync(httpResponse, new { error = "Missing or invalid Authorization header" });
                return;
            }

            string token = authHeader.Substring("Bearer ".Length);
            if (!_serverAuthService.ValidateToken(token))
            {
                httpResponse.StatusCode = 403;
                await JsonSerializationHelper.WriteJsonAsync(httpResponse, new { error = "Invalid or expired token" });
                return;
            }

            string username = _serverAuthService.GetTokenSubject(token) ?? "unknown";
            httpResponse.StatusCode = 200;
            await JsonSerializationHelper.WriteJsonAsync(httpResponse, new { username, message = $"Welcome to your profile, {username}!" });
        }
    }
}
