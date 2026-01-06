using Models.DTOs;
using MRP.Models;
using MRP_DAL.Interfaces;
using MRP_Server.Http.Helpers;
using MRP_Server.Services;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace MRP_Server.Http.Controllers
{

    public class MediaController
    {
        private readonly IMediaEntryRepository _mediaRepository;
        private readonly IRatingRepository _ratingRepository;
        private readonly IUserRepository _userRepository;
        private readonly ServerAuthService _serverAuthService;

        public MediaController(IMediaEntryRepository mediaRepo, IRatingRepository ratingRepository, IUserRepository userRepository, ServerAuthService serverAuthService)
        {
            _mediaRepository = mediaRepo;
            _ratingRepository = ratingRepository;
            _userRepository = userRepository;
            _serverAuthService = serverAuthService;
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
                    case "POST" when RouteHelper.TryGetIdBeforeSuffix(path, "/api/media/", "favorite", out var mediaIdFavPost):
                        await HandleFavoriteAsync(req, res, mediaIdFavPost);
                        return;

                    case "DELETE" when RouteHelper.TryGetIdBeforeSuffix(path, "/api/media/", "favorite", out var mediaIdFavDel):
                        await HandleUnfavoriteAsync(req, res, mediaIdFavDel);
                        return;

                    case "GET" when path.Equals("/api/media", StringComparison.OrdinalIgnoreCase):
                        await HandleListAsync(req, res);
                        return;

                    case "POST" when path.Equals("/api/media", StringComparison.OrdinalIgnoreCase):
                        await HandleCreateAsync(req, res);
                        return;

                    case "GET" when RouteHelper.TryGetIdAfterPrefix(path, "/api/media/", out var mediaIdGet):
                        await HandleGetByIdAsync(req, res, mediaIdGet);
                        return;

                    case "PUT" when RouteHelper.TryGetIdAfterPrefix(path, "/api/media/", out var mediaIdPut):
                        await HandleUpdateAsync(req, res, mediaIdPut);
                        return;

                    case "DELETE" when RouteHelper.TryGetIdAfterPrefix(path, "/api/media/", out var mediaIdDel):
                        await HandleDeleteAsync(req, res, mediaIdDel);
                        return;

                    case "POST" when RouteHelper.TryGetIdBeforeSuffix(path, "/api/media/", "rate", out var mediaIdRate):
                        await HandleRateAsync(req, res, mediaIdRate);
                        return;

                    default:
                        res.StatusCode = 404;
                        await JsonSerializationHelper.WriteJsonAsync(res, new { error = "Endpoint not found" });
                        return;
                }
            }
            catch (Exception ex)
            {
                res.StatusCode = 500;
                await JsonSerializationHelper.WriteJsonAsync(res, new { error = ex.Message });
            }
            finally
            {
                res.Close();
            }
        }
        private async Task HandleFavoriteAsync(HttpListenerRequest req, HttpListenerResponse res, int mediaId)
        {
            var userId = await RequireUserIdAsync(req, res);
            if (userId == null) return;

            var owner = await _mediaRepository.GetOwnerIdByMediaIdAsync(mediaId);
            if (owner == null)
            {
                res.StatusCode = 404;
                await JsonSerializationHelper.WriteJsonAsync(res, new { error = "Media entry not found" });
                return;
            }

            var ok = await _mediaRepository.FavouriteAsync(userId.Value, mediaId);

            if (!ok)
            {
                res.StatusCode = 409;
                await JsonSerializationHelper.WriteJsonAsync(res, new { error = "Already favorited" });
                return;
            }

            res.StatusCode = 200;
            await JsonSerializationHelper.WriteJsonAsync(res, new { success = true });
        }

        private async Task HandleUnfavoriteAsync(HttpListenerRequest req, HttpListenerResponse res, int mediaId)
        {
            var userId = await RequireUserIdAsync(req, res);
            if (userId == null) return;

            var owner = await _mediaRepository.GetOwnerIdByMediaIdAsync(mediaId);
            if (owner == null)
            {
                res.StatusCode = 404;
                await JsonSerializationHelper.WriteJsonAsync(res, new { error = "Media entry not found" });
                return;
            }

            var ok = await _mediaRepository.UnfavouriteAsync(userId.Value, mediaId);

            if (!ok)
            {
                res.StatusCode = 404;
                await JsonSerializationHelper.WriteJsonAsync(res, new { error = "Not favorited" });
                return;
            }

            res.StatusCode = 204;
        }

        private async Task HandleListAsync(HttpListenerRequest req, HttpListenerResponse res)
        {
            var title = req.QueryString["title"] ?? req.QueryString["q"];
            var genre = req.QueryString["genre"];

            int? type = null;
            var typeRaw = req.QueryString["mediaType"] ?? req.QueryString["type"];
            if (int.TryParse(typeRaw, out var t)) type = t;

            var year = req.QueryString["releaseYear"] ?? req.QueryString["year"];

            int? age = null;
            var ageRaw = req.QueryString["ageRestriction"] ?? req.QueryString["age"];
            if (int.TryParse(ageRaw, out var a)) age = a;

            double? minScore = null;
            var scoreRaw = req.QueryString["rating"] ?? req.QueryString["minScore"];
            if (double.TryParse(scoreRaw, out var s)) minScore = s;

            var sortBy = (req.QueryString["sortBy"] ?? req.QueryString["sort"] ?? "title").ToLowerInvariant();
            var sortOrder = (req.QueryString["order"] ?? "asc").ToLowerInvariant();
            if (sortOrder != "asc" && sortOrder != "desc") sortOrder = "asc";

            int? userId = null; 
            var media = await _mediaRepository.SearchAsync(title, genre, type, year, age, minScore, sortBy, sortOrder, userId);

            res.StatusCode = 200;
            await JsonSerializationHelper.WriteJsonAsync(res, media);
        }

        private async Task HandleGetByIdAsync(HttpListenerRequest req, HttpListenerResponse res, int mediaId)
        {
            var userId = await RequireUserIdAsync(req, res);
            if (userId == null) return;

            var media = await _mediaRepository.GetByIdAsync(mediaId, userId.Value);
            if (media == null) { res.StatusCode = 404; return; }

            media.Genres = await _mediaRepository.GetGenresAsync(mediaId);

            res.StatusCode = 200;
            await JsonSerializationHelper.WriteJsonAsync(res, media);
        }
        private async Task HandleCreateAsync(HttpListenerRequest req, HttpListenerResponse res)
        {
            var userId = await RequireUserIdAsync(req, res);
            if (userId == null) return;

            var entry = await ReadBodyAsync<MediaEntryDTO>(req);
            if (entry == null || string.IsNullOrWhiteSpace(entry.Title))
            {
                res.StatusCode = 400;
                await JsonSerializationHelper.WriteJsonAsync(res, new { error = "Invalid body" });
                return;
            }

            entry.OwnerId = userId.Value;

            var id = await _mediaRepository.CreateAsync(entry);

            if (entry.Genres != null && entry.Genres.Count > 0)
                await _mediaRepository.SetGenresAsync(id, entry.Genres);

            res.StatusCode = 201;
            await JsonSerializationHelper.WriteJsonAsync(res, new { id });
        }

        private async Task HandleUpdateAsync(HttpListenerRequest req, HttpListenerResponse res, int mediaId)
        {
            var userId = await RequireUserIdAsync(req, res);
            if (userId == null) return;

            var ownerId = await _mediaRepository.GetOwnerIdByMediaIdAsync(mediaId);
            if (ownerId == null)
            {
                res.StatusCode = 404;
                await JsonSerializationHelper.WriteJsonAsync(res, new { error = "Media entry not found" });
                return;
            }

            if (ownerId.Value != userId.Value)
            {
                res.StatusCode = 403;
                await JsonSerializationHelper.WriteJsonAsync(res, new { error = "Forbidden" });
                return;
            }

            var dto = await ReadBodyAsync<MediaEntryDTO>(req);
            if (dto == null || string.IsNullOrWhiteSpace(dto.Title))
            {
                res.StatusCode = 400;
                await JsonSerializationHelper.WriteJsonAsync(res, new { error = "Invalid body" });
                return;
            }

            dto.Id = mediaId;
            dto.OwnerId = userId.Value;

            var ok = await _mediaRepository.UpdateAsync(dto);
            if (!ok)
            {
                res.StatusCode = 404;
                await JsonSerializationHelper.WriteJsonAsync(res, new { error = "Media entry not found" });
                return;
            }

            if (dto.Genres != null)
                await _mediaRepository.SetGenresAsync(mediaId, dto.Genres);

            res.StatusCode = 200;
        }

        private async Task HandleDeleteAsync(HttpListenerRequest req, HttpListenerResponse res, int mediaId)
        {
            var userId = await RequireUserIdAsync(req, res);
            if (userId == null) return;

            var deleted = await _mediaRepository.DeleteAsync(mediaId, userId.Value);
            if (!deleted)
            {
                res.StatusCode = 404;
                await JsonSerializationHelper.WriteJsonAsync(res, new { error = "Media entry not found" });
                return;
            }

            res.StatusCode = 204;
        }

        private async Task HandleRateAsync(HttpListenerRequest req, HttpListenerResponse res, int mediaId)
        {
            var userId = await RequireUserIdAsync(req, res);
            if (userId == null) return;

            var owner = await _mediaRepository.GetOwnerIdByMediaIdAsync(mediaId);
            if (owner == null)
            {
                res.StatusCode = 404;
                await JsonSerializationHelper.WriteJsonAsync(res, new { error = "Media entry not found" });
                return;
            }

            var rating = await ReadBodyAsync<Rating>(req);
            if (rating == null || rating.StarValue < 1 || rating.StarValue > 5)
            {
                res.StatusCode = 400;
                await JsonSerializationHelper.WriteJsonAsync(res, new { error = "Invalid rating body" });
                return;
            }

            rating.MediaEntryId = mediaId;
            rating.OwnerId = userId.Value;
            rating.Timestamp = DateTime.UtcNow;
            rating.IsCommentVisible = false;

            var id = await _ratingRepository.CreateAsync(rating);

            res.StatusCode = 201;
            await JsonSerializationHelper.WriteJsonAsync(res, new { id });
        }

        private async Task<int?> RequireUserIdAsync(HttpListenerRequest req, HttpListenerResponse res)
        {
            var (ok, username) = await AuthHelper.ValidateAndExtractTokenAsync(req, res, _serverAuthService);
            if (!ok || string.IsNullOrWhiteSpace(username)) return null;

            var userId = await _userRepository.GetUserIdByUsernameAsync(username);
            if (userId == null)
            {
                res.StatusCode = 404;
                await JsonSerializationHelper.WriteJsonAsync(res, new { error = "User not found" });
                return null;
            }

            return userId.Value;
        }

        private static async Task<T?> ReadBodyAsync<T>(HttpListenerRequest req)
        {
            using var reader = new StreamReader(req.InputStream, req.ContentEncoding);
            var body = await reader.ReadToEndAsync();
            if (string.IsNullOrWhiteSpace(body)) return default;
            return JsonConvert.DeserializeObject<T>(body);
        }
    }
}
