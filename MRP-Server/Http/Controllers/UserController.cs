using Models.DTOs;
using MRP_DAL.Interfaces;
using MRP_Server.Http.Helpers;
using MRP_Server.Services;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace MRP_Server.Http.Controllers
{
    public class UserController
    {
        private readonly ServerAuthService _auth;
        private readonly IUserRepository _users;
        private readonly IRatingRepository _ratings;
        private readonly IRecommendationRepository _reco;
        private readonly IMediaEntryRepository _media;

        public UserController(ServerAuthService authService, IUserRepository userRepository, IRatingRepository ratingRepository, IRecommendationRepository recommendationRepository, IMediaEntryRepository mediaRepository)
        {
            _auth = authService;
            _users = userRepository;
            _ratings = ratingRepository;
            _reco = recommendationRepository;
            _media = mediaRepository;
        }


        public async Task HandleAsync(HttpListenerContext ctx)
        {
            var req = ctx.Request;
            var res = ctx.Response;
            var path = (req.Url?.AbsolutePath ?? "").TrimEnd('/');

            try
            {
                switch (req.HttpMethod.ToUpperInvariant())
                {
                    case "POST" when path.Equals("/api/users/register", StringComparison.OrdinalIgnoreCase):
                        await RegisterAsync(req, res);
                        return;

                    case "POST" when path.Equals("/api/users/login", StringComparison.OrdinalIgnoreCase):
                        await LoginAsync(req, res);
                        return;

                    case "GET" when RouteHelper.TryGetIdBeforeSuffix(path, "/api/users/", "profile", out var userIdGet):
                        await HandleGetProfileAsync(req, res, userIdGet);
                        return;

                    case "PUT" when RouteHelper.TryGetIdBeforeSuffix(path, "/api/users/", "profile", out var userIdPut):
                        await HandlePutProfileAsync(req, res, userIdPut);
                        return;

                    case "GET" when RouteHelper.TryGetIdBeforeSuffix(path, "/api/users/", "ratings", out var uidRatings):
                        await HandleGetUserRatingsAsync(req, res, uidRatings);
                        return;

                    case "GET" when RouteHelper.TryGetIdBeforeSuffix(path, "/api/users/", "favorites", out var uidFavs):
                        await HandleGetUserFavoritesAsync(req, res, uidFavs);
                        return;

                    case "GET" when RouteHelper.TryGetIdBeforeSuffix(path, "/api/users/", "recommendations", out var uidReco):
                        await HandleGetUserRecommendationsAsync(req, res, uidReco);
                        return;

                    default:
                        res.StatusCode = 404;
                        await JsonSerializationHelper.WriteJsonAsync(res, new { error = "Endpoint not found" });
                        return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UserController error: {ex}");
                res.StatusCode = 500;
                await JsonSerializationHelper.WriteJsonAsync(res, new { error = "Internal Server Error" });
            }
            finally
            {
                res.Close();
            }
        }
        private async Task<bool> EnsureSameUserAsync(HttpListenerRequest req, HttpListenerResponse res, int userId)
        {
            var tokenUserId = await RequireTokenUserIdAsync(req, res);
            if (tokenUserId == null) return false;

            if (tokenUserId.Value != userId)
            {
                res.StatusCode = 403;
                await JsonSerializationHelper.WriteJsonAsync(res, new { error = "Forbidden" });
                return false;
            }

            return true;
        }

        private async Task HandleGetUserRatingsAsync(HttpListenerRequest req, HttpListenerResponse res, int userId)
        {
            if (!await EnsureSameUserAsync(req, res, userId)) return;

            var ratings = await _ratings.GetByUserAsync(userId);
            res.StatusCode = 200;
            await JsonSerializationHelper.WriteJsonAsync(res, ratings);
        }

        private async Task HandleGetUserFavoritesAsync(HttpListenerRequest req, HttpListenerResponse res, int userId)
        {
            if (!await EnsureSameUserAsync(req, res, userId)) return;

            var favorites = await _media.GetFavoriteMediaIdsAsync(userId);

            res.StatusCode = 200;
            await JsonSerializationHelper.WriteJsonAsync(res, favorites);
        }

        private async Task HandleGetUserRecommendationsAsync(HttpListenerRequest req, HttpListenerResponse res, int userId)
        {
            if (!await EnsureSameUserAsync(req, res, userId)) return;

            var type = (req.QueryString["type"] ?? "content").ToLowerInvariant();
            var recos = await _reco.GetRecommendationsAsync(userId, type);

            res.StatusCode = 200;
            await JsonSerializationHelper.WriteJsonAsync(res, recos);
        }

        private async Task RegisterAsync(HttpListenerRequest req, HttpListenerResponse res)
        {
            var creds = await JsonSerializationHelper.ReadJsonAsync<LoginDTO>(req);

            if (creds == null || string.IsNullOrWhiteSpace(creds.Username) || string.IsNullOrWhiteSpace(creds.Password))
            {
                res.StatusCode = 400;
                await JsonSerializationHelper.WriteJsonAsync(res, new { error = "Invalid JSON body" });
                return;
            }

            var created = await _auth.RegisterAsync(creds.Username, creds.Password);

            res.StatusCode = created ? 201 : 400;
            await JsonSerializationHelper.WriteJsonAsync(res, new { success = created });
        }

        private async Task LoginAsync(HttpListenerRequest req, HttpListenerResponse res)
        {
            var creds = await JsonSerializationHelper.ReadJsonAsync<LoginDTO>(req);

            if (creds == null || string.IsNullOrWhiteSpace(creds.Username) || string.IsNullOrWhiteSpace(creds.Password))
            {
                res.StatusCode = 400;
                await JsonSerializationHelper.WriteJsonAsync(res, new { error = "Invalid request body" });
                return;
            }

            var token = await _auth.TryLoginAsync(creds.Username, creds.Password);

            if (token == null)
            {
                res.StatusCode = 401;
                await JsonSerializationHelper.WriteJsonAsync(res, new { error = "Invalid credentials" });
                return;
            }

            res.StatusCode = 200;
            await JsonSerializationHelper.WriteJsonAsync(res, new { token });
        }

        private async Task<int?> RequireTokenUserIdAsync(HttpListenerRequest req, HttpListenerResponse res)
        {
            var (ok, username) = await AuthHelper.ValidateAndExtractTokenAsync(req, res, _auth);
            if (!ok || string.IsNullOrWhiteSpace(username)) return null;

            var userId = await _users.GetUserIdByUsernameAsync(username);
            if (userId == null)
            {
                res.StatusCode = 404;
                await JsonSerializationHelper.WriteJsonAsync(res, new { error = "User not found" });
                return null;
            }

            return userId.Value;
        }

        private async Task HandleGetProfileAsync(HttpListenerRequest req, HttpListenerResponse res, int userId)
        {
            var tokenUserId = await RequireTokenUserIdAsync(req, res);
            if (tokenUserId == null) return;

            if (tokenUserId.Value != userId)
            {
                res.StatusCode = 403;
                await JsonSerializationHelper.WriteJsonAsync(res, new { error = "Forbidden" });
                return;
            }

            var username = await _users.GetUsernameByIdAsync(userId);
            if (string.IsNullOrWhiteSpace(username))
            {
                res.StatusCode = 404;
                await JsonSerializationHelper.WriteJsonAsync(res, new { error = "User not found" });
                return;
            }

            var stats = await _users.GetUserProfileStatsAsync(username);
            if (stats == null)
            {
                res.StatusCode = 404;
                await JsonSerializationHelper.WriteJsonAsync(res, new { error = "User not found" });
                return;
            }


            var profile = await _users.GetProfileAsync(userId);
            res.StatusCode = 200;
            await JsonSerializationHelper.WriteJsonAsync(res, new
            {
                stats,
                email = profile?.Email,
                favoriteGenre = profile?.FavoriteGenre
            });
            return;
        }

        private async Task HandlePutProfileAsync(HttpListenerRequest req, HttpListenerResponse res, int userId)
        {
            var tokenUserId = await RequireTokenUserIdAsync(req, res);
            if (tokenUserId == null) return;

            if (tokenUserId.Value != userId)
            {
                res.StatusCode = 403;
                await JsonSerializationHelper.WriteJsonAsync(res, new { error = "Forbidden" });
                return;
            }

            var body = await ReadBodyAsJObjectAsync(req);
            if (body == null)
            {
                res.StatusCode = 400;
                await JsonSerializationHelper.WriteJsonAsync(res, new { error = "Invalid JSON body" });
                return;
            }

            string? email = body["email"]?.Type == JTokenType.Null ? null : body["email"]?.ToString();

            string? favoriteGenre = body["favoriteGenre"]?.Type == JTokenType.Null ? null : body["favoriteGenre"]?.ToString();

            var ok = await _users.UpdateProfileAsync(userId, email, favoriteGenre);
            if (!ok)
            {
                res.StatusCode = 404;
                await JsonSerializationHelper.WriteJsonAsync(res, new { error = "User not found" });
                return;
            }

            res.StatusCode = 200;
            await JsonSerializationHelper.WriteJsonAsync(res, new { success = true });
        }

        private static async Task<JObject?> ReadBodyAsJObjectAsync(HttpListenerRequest req)
        {
            using var reader = new StreamReader(req.InputStream, req.ContentEncoding);
            var raw = await reader.ReadToEndAsync();
            if (string.IsNullOrWhiteSpace(raw)) return null;

            try { return JObject.Parse(raw); }
            catch { return null; }
        }
    }
}
