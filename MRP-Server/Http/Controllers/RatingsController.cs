using MRP.Models;
using MRP_DAL.Interfaces;
using MRP_Server.Http.Helpers;
using MRP_Server.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MRP_Server.Http.Controllers
{

    public class RatingsController
    {
        private readonly IRatingRepository _ratingRepository;
        private readonly IUserRepository _userRepository;
        private readonly ServerAuthService _serverAuthService;

        public RatingsController(IRatingRepository ratingRepository, IUserRepository userRepository, ServerAuthService serverAuthService)
        {
            _ratingRepository = ratingRepository;
            _userRepository = userRepository;
            _serverAuthService = serverAuthService;
        }

        public async Task HandleAsync(HttpListenerContext listenerContext)
        {
            var httpRequest = listenerContext.Request;
            var httpResponse = listenerContext.Response;
            var path = httpRequest.Url?.AbsolutePath ?? "";
            try
            {
                switch (httpRequest.HttpMethod)
                {
                    case "POST" when path.EndsWith("/like", StringComparison.OrdinalIgnoreCase):
                        await HandleLikeAsync(httpRequest, httpResponse);
                        break;
                    case "DELETE":
                        await HandleUnlikeAsync(httpRequest, httpResponse);
                        break;
                    case "PATCH": // partial change
                        await HandleConfirmAsync(httpRequest, httpResponse);
                        break;
                    case "GET":
                        await GetRatingsAsync(httpRequest, httpResponse);
                        break;
                    case "POST":
                        await CreateRatingAsync(httpRequest, httpResponse);
                        break;
                    default:
                        httpResponse.StatusCode = 405;
                        await JsonSerializationHelper.WriteJsonAsync(httpResponse, new { error = "Method not allowed" });
                        break;
                }
            }
            catch (Exception ex)
            {
                httpResponse.StatusCode = 500;
                await JsonSerializationHelper.WriteJsonAsync(httpResponse, new { error = ex.Message });
            }
            finally { httpResponse.Close(); }
        }

        private async Task GetRatingsAsync(HttpListenerRequest httpRequest, HttpListenerResponse httpResponse)
        {
            if (!int.TryParse(httpRequest.QueryString["mediaId"], out var mediaId))
            {
                httpResponse.StatusCode = 400;
                await JsonSerializationHelper.WriteJsonAsync(httpResponse, new { error = "Missing or invalid mediaId" });
                return;
            }

            var list = await _ratingRepository.GetByMediaAsync(mediaId);
            httpResponse.StatusCode = 200;
            await JsonSerializationHelper.WriteJsonAsync(httpResponse, list);
        }
        private async Task CreateRatingAsync(HttpListenerRequest httpRequest, HttpListenerResponse httpResponse)
        {
            var (isValid, username) = AuthHelper.ValidateAndExtractToken(httpRequest, httpResponse, _serverAuthService);
            if (!isValid || string.IsNullOrEmpty(username)) return;

            int? userId = await _userRepository.GetUserIdByUsernameAsync(username);
            if (userId == null)
            {
                httpResponse.StatusCode = 404;
                await JsonSerializationHelper.WriteJsonAsync(httpResponse, new { error = "User not found" });
                return;
            }

            var dto = await JsonSerializationHelper.ReadJsonAsync<Rating>(httpRequest);
            if (dto == null || dto.StarValue < 1 || dto.StarValue > 5)
            {
                httpResponse.StatusCode = 400;
                await JsonSerializationHelper.WriteJsonAsync(httpResponse, new { error = "Invalid rating data" });
                return;
            }

            dto.OwnerId = userId.Value;
            int id = await _ratingRepository.CreateAsync(dto);
            httpResponse.StatusCode = 201;
            await JsonSerializationHelper.WriteJsonAsync(httpResponse, new { id });
        }
        private async Task HandleLikeAsync(HttpListenerRequest httpRequest, HttpListenerResponse httpResponse)
        {
            var (isValid, username) = AuthHelper.ValidateAndExtractToken(httpRequest, httpResponse, _serverAuthService);
            if (!isValid || string.IsNullOrEmpty(username)) return;

            int? userId = await _userRepository.GetUserIdByUsernameAsync(username);
            if (userId == null)
            {
                httpResponse.StatusCode = 404;
                await JsonSerializationHelper.WriteJsonAsync(httpResponse, new { error = "User not found" });
                return;
            }

            if (!int.TryParse(httpRequest.QueryString["ratingId"], out var ratingId))
            {
                httpResponse.StatusCode = 400;
                await JsonSerializationHelper.WriteJsonAsync(httpResponse, new { error = "Missing or invalid ratingId" });
                return;
            }

            bool liked = await _ratingRepository.LikeRatingAsync(ratingId, userId.Value);
            httpResponse.StatusCode = liked ? 200 : 409;
            await JsonSerializationHelper.WriteJsonAsync(httpResponse, new { success = liked });
        }

        private async Task HandleUnlikeAsync(HttpListenerRequest httpRequest, HttpListenerResponse httpResponse)
        {
            var (isValid, username) = AuthHelper.ValidateAndExtractToken(httpRequest, httpResponse, _serverAuthService);
            if (!isValid || string.IsNullOrEmpty(username)) return;

            int? userId = await _userRepository.GetUserIdByUsernameAsync(username);
            if (userId == null)
            {
                httpResponse.StatusCode = 404;
                await JsonSerializationHelper.WriteJsonAsync(httpResponse, new { error = "User not found" });
                return;
            }

            if (!int.TryParse(httpRequest.QueryString["ratingId"], out var ratingId))
            {
                httpResponse.StatusCode = 400;
                await JsonSerializationHelper.WriteJsonAsync(httpResponse, new { error = "Missing or invalid ratingId" });
                return;
            }

            bool unliked = await _ratingRepository.UnlikeRatingAsync(ratingId, userId.Value);
            httpResponse.StatusCode = unliked ? 200 : 404;
            await JsonSerializationHelper.WriteJsonAsync(httpResponse, new { success = unliked });
        }

        private async Task HandleConfirmAsync(HttpListenerRequest httpRequest, HttpListenerResponse httpResponse)
        {
            var (isValid, username) = AuthHelper.ValidateAndExtractToken(httpRequest, httpResponse, _serverAuthService);
            if (!isValid || string.IsNullOrEmpty(username)) return;

            int? userId = await _userRepository.GetUserIdByUsernameAsync(username);
            if (userId == null)
            {
                httpResponse.StatusCode = 404;
                await JsonSerializationHelper.WriteJsonAsync(httpResponse, new { error = "User not found" });
                return;
            }

            if (!int.TryParse(httpRequest.QueryString["ratingId"], out var ratingId))
            {
                httpResponse.StatusCode = 400;
                await JsonSerializationHelper.WriteJsonAsync(httpResponse, new { error = "Missing or invalid ratingId" });
                return;
            }

            bool confirmed = await _ratingRepository.ConfirmCommentAsync(ratingId, userId.Value);
            httpResponse.StatusCode = confirmed ? 200 : 403;
            await JsonSerializationHelper.WriteJsonAsync(httpResponse, new { success = confirmed });
        }
    }
}
