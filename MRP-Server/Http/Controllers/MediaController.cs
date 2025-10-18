using Models.DTOs;
using MRP.Models;
using MRP_DAL;
using MRP_DAL.Interfaces;
using MRP_DAL.Repositories;
using MRP_Server.Http.Helpers;
using MRP_Server.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MRP_Server.Http.Controllers
{
    public class MediaController
    {
        private readonly IMediaEntryRepository _mediaRepository;
        private readonly ServerAuthService _serverAuthService;
        private readonly IUserRepository _userRepository;



        public MediaController(IMediaEntryRepository mediaRepo, ServerAuthService serverAuthService, IUserRepository userRepository)
        {
            _mediaRepository = mediaRepo;
            _serverAuthService = serverAuthService;
            _userRepository = userRepository;
        }

        public async Task HandleAsync(HttpListenerContext listenerContext)
        {
            var httpRequest = listenerContext.Request;
            var httpResponse = listenerContext.Response;

            try
            {
                switch (httpRequest.HttpMethod)
                {
                    case "GET":
                        await HandleGetAsync(httpResponse);
                        break;

                    case "POST":
                        await HandleCreateAsync(httpRequest, httpResponse);
                        break;

                    case "DELETE":
                        await HandleDeleteAsync(httpRequest, httpResponse);
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
            finally
            {
                httpResponse.Close();
            }
        }

        private async Task HandleGetAsync(HttpListenerResponse httpResponse)
        {
            var media = await _mediaRepository.GetAllAsync();
            httpResponse.StatusCode = 200;
            await JsonSerializationHelper.WriteJsonAsync(httpResponse, media);
        }

        private async Task HandleCreateAsync(HttpListenerRequest httpRequest, HttpListenerResponse httpResponse)
        {
            var (isValid, username) = AuthHelper.ValidateAndExtractToken(httpRequest, httpResponse, _serverAuthService);
            if (!isValid || string.IsNullOrEmpty(username)) return;

            int? userId = await _userRepository.GetUserIdByUsernameAsync(username);

            using var reader = new StreamReader(httpRequest.InputStream, httpRequest.ContentEncoding);
            var body = await reader.ReadToEndAsync();
            var entry = JsonConvert.DeserializeObject<MediaEntryDTO>(body);

            if (entry == null || string.IsNullOrWhiteSpace(entry.Title))
            {
                httpResponse.StatusCode = 400;
                await JsonSerializationHelper.WriteJsonAsync(httpResponse, new { error = "Invalid body" });
                return;
            }

            entry.OwnerId = userId;

            var id = await _mediaRepository.CreateAsync(entry);
            httpResponse.StatusCode = 201;
            await JsonSerializationHelper.WriteJsonAsync(httpResponse, new { id });
        }

        private async Task HandleDeleteAsync(HttpListenerRequest httpRequest, HttpListenerResponse httpResponse)
        {
            var (isValid, username) = AuthHelper.ValidateAndExtractToken(httpRequest, httpResponse, _serverAuthService);
            if (!isValid || string.IsNullOrEmpty(username)) return;

            int? userId = await _userRepository.GetUserIdByUsernameAsync(username);

            var query = httpRequest.QueryString;
            if (!int.TryParse(query["id"], out int mediaId))
            {
                httpResponse.StatusCode = 400;
                await JsonSerializationHelper.WriteJsonAsync(httpResponse, new { error = "Missing or invalid id" });
                return;
            }

            bool deleted = await _mediaRepository.DeleteAsync(mediaId, (int)userId);

            httpResponse.StatusCode = deleted ? 200 : 404;
            await JsonSerializationHelper.WriteJsonAsync(httpResponse, new { success = deleted });
        }

    }
}
