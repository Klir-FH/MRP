using MRP.Models;
using MRP_DAL.Interfaces;
using MRP_Server.Http.Helpers;
using MRP_Server.Services;
using System;
using System.Net;
using System.Threading.Tasks;

namespace MRP_Server.Http.Controllers
{
    public class RatingsController
    {
        private readonly IRatingRepository _ratings;
        private readonly IUserRepository _users;
        private readonly ServerAuthService _auth;

        public RatingsController(IRatingRepository ratingRepository, IUserRepository userRepository, ServerAuthService serverAuthService)
        {
            _ratings = ratingRepository;
            _users = userRepository;
            _auth = serverAuthService;
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
                    case "PUT" when RouteHelper.TryGetIdAfterPrefix(path, "/api/ratings/", out var ratingIdPut):
                        await HandleUpdateAsync(req, res, ratingIdPut);
                        return;

                    case "DELETE" when RouteHelper.TryGetIdAfterPrefix(path, "/api/ratings/", out var ratingIdDel):
                        await HandleDeleteAsync(req, res, ratingIdDel);
                        return;

                    case "POST" when RouteHelper.TryGetIdBeforeSuffix(path, "/api/ratings/", "confirm", out var ratingIdConfirm):
                        await HandleConfirmAsync(req, res, ratingIdConfirm);
                        return;

                    case "POST" when RouteHelper.TryGetIdBeforeSuffix(path, "/api/ratings/", "like", out var ratingIdLike):
                        await HandleLikeAsync(req, res, ratingIdLike);
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

        private async Task HandleUpdateAsync(HttpListenerRequest req, HttpListenerResponse res, int ratingId)
        {
            var userId = await RequireTokenUserIdAsync(req, res);
            if (userId == null) return;

            var dto = await JsonSerializationHelper.ReadJsonAsync<Rating>(req);
            if (dto == null || dto.StarValue < 1 || dto.StarValue > 5)
            {
                res.StatusCode = 400;
                await JsonSerializationHelper.WriteJsonAsync(res, new { error = "Invalid rating data" });
                return;
            }

            var ok = await _ratings.UpdateAsync(ratingId, userId.Value, dto.StarValue, dto.Comment);

            res.StatusCode = ok ? 200 : 403;
            await JsonSerializationHelper.WriteJsonAsync(res, new { success = ok });
        }

        private async Task HandleDeleteAsync(HttpListenerRequest req, HttpListenerResponse res, int ratingId)
        {
            var userId = await RequireTokenUserIdAsync(req, res);
            if (userId == null) return;

            var ok = await _ratings.DeleteAsync(ratingId, userId.Value);

            res.StatusCode = ok ? 204 : 403;
            await JsonSerializationHelper.WriteJsonAsync(res, new { success = ok });
        }

        private async Task HandleConfirmAsync(HttpListenerRequest req, HttpListenerResponse res, int ratingId)
        {
            var userId = await RequireTokenUserIdAsync(req, res);
            if (userId == null) return;

            var ok = await _ratings.ConfirmCommentAsync(ratingId, userId.Value);

            res.StatusCode = ok ? 200 : 403;
            await JsonSerializationHelper.WriteJsonAsync(res, new { success = ok });
        }

        private async Task HandleLikeAsync(HttpListenerRequest req, HttpListenerResponse res, int ratingId)
        {
            var userId = await RequireTokenUserIdAsync(req, res);
            if (userId == null) return;

            var ok = await _ratings.LikeRatingAsync(ratingId, userId.Value);

            res.StatusCode = ok ? 200 : 409;
            await JsonSerializationHelper.WriteJsonAsync(res, new { success = ok });
        }
    }
}
