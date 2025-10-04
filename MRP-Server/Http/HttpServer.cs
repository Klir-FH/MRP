using MRP_Server.Services;
using System.Net;
using System.Text;
using Newtonsoft.Json;

namespace MRP_Server.Http
{
    public class HttpServer
    {
        private readonly HttpListener _listener = new();
        private readonly ServerAuthService _authService;

        public HttpServer(ServerAuthService authService) => _authService = authService;

        public async Task StartAsync(string prefix)
        {
            _listener.Prefixes.Add(prefix);
            _listener.Start();
            while (true)
            {
                var context = await _listener.GetContextAsync();
                _ = HandleRequestAsync(context);
            }
        }

        private async Task HandleRequestAsync(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

            try
            {
                var path = request.Url.AbsolutePath;
                var method = request.HttpMethod;

                if (method == "POST" && path == "/api/users/register")
                {
                    await HandleRegisterAsync(request, response);
                }
                else if (method == "POST" && path == "/api/users/login")
                {
                    await HandleLoginAsync(request, response);
                }
                else if (method == "GET" && path.StartsWith("/api/users/"))
                {
                    await HandleProfileAsync(request, response);
                }
                else
                {
                    response.StatusCode = 404;
                    await WriteJsonAsync(response, new { error = "Endpoint not found" });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex}");
                response.StatusCode = 500;
                await WriteJsonAsync(response, new { error = "Internal Server Error" });
            }
            finally
            {
                response.Close();
            }
        }

        private async Task HandleRegisterAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            var data = await ReadJsonAsync<Dictionary<string, string>>(request);

            if (data == null
                || !data.ContainsKey("username")
                || !data.ContainsKey("password"))
            {
                response.StatusCode = 400;
                await WriteJsonAsync(response, new { error = "Invalid JSON body" });
                return;
            }

            bool created = await _authService.RegisterAsync(
                data["username"],
                data["password"]
            );

            response.StatusCode = created ? 201 : 400;
            await WriteJsonAsync(response, new { success = created });
        }

        private async Task HandleLoginAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            var data = await ReadJsonAsync<Dictionary<string, string>>(request);

            if (data == null
                || !data.ContainsKey("username")
                || !data.ContainsKey("password"))
            {
                response.StatusCode = 400;
                await WriteJsonAsync(response, new { error = "Invalid JSON body" });
                return;
            }

            var token = await _authService.TryLoginAsync(data["username"], data["password"]);
            if (token == null)
            {
                response.StatusCode = 401;
                await WriteJsonAsync(response, new { error = "Invalid credentials" });
                return;
            }

            response.StatusCode = 200;
            await WriteJsonAsync(response, new { token });
        }

        private async Task HandleProfileAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            string? authHeader = request.Headers["Authorization"];
            if (authHeader == null || !authHeader.StartsWith("Bearer "))
            {
                response.StatusCode = 401;
                await WriteJsonAsync(response, new { error = "Missing or invalid Authorization header" });
                return;
            }

            string token = authHeader.Substring("Bearer ".Length);
            if (!_authService.ValidateToken(token))
            {
                response.StatusCode = 403;
                await WriteJsonAsync(response, new { error = "Invalid or expired token" });
                return;
            }

            string username = _authService.GetTokenSubject(token) ?? "unknown";

            response.StatusCode = 200;
            await WriteJsonAsync(response, new
            {
                username,
                message = $"Welcome to your profile, {username}!"
            });
        }

        private static async Task<T?> ReadJsonAsync<T>(HttpListenerRequest request)
        {
            using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
            var body = await reader.ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(body))
                return default;

            try
            {
                return JsonConvert.DeserializeObject<T>(body);
            }
            catch (JsonException)
            {
                return default;
            }
        }

        private static async Task WriteJsonAsync(HttpListenerResponse response, object obj)
        {
            response.ContentType = "application/json";
            string json = JsonConvert.SerializeObject(obj);
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            await response.OutputStream.WriteAsync(bytes);
        }
    }
}
