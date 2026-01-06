using MRP_Server.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MRP_Server.Http.Helpers
{
    public static class AuthHelper
    {
        public static async Task<(bool IsValid, string? username)> ValidateAndExtractTokenAsync(HttpListenerRequest request, HttpListenerResponse response, ServerAuthService authService)
        {
            var authHeader = request.Headers["Authentication"];

            if (string.IsNullOrWhiteSpace(authHeader))
            {
                await Write401(response, "Missing Authentication header");
                return (false, null);
            }

            if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                await Write401(response, "Invalid authentication scheme");
                return (false, null);
            }

            var token = authHeader.Substring("Bearer ".Length).Trim();
            if (string.IsNullOrWhiteSpace(token))
            {
                await Write401(response, "Missing bearer token");
                return (false, null);
            }

            if (!authService.ValidateToken(token))
            {
                await Write401(response, "Invalid or expired token");
                return (false, null);
            }

            var username = authService.GetTokenSubject(token);
            if (string.IsNullOrWhiteSpace(username))
            {
                await Write401(response, "User not found");
                return (false, null);
            }

            return (true, username);
        }

        private static async Task Write401(HttpListenerResponse response, string message)
        {
            response.StatusCode = 401;
            response.ContentType = "application/json";

            await JsonSerializationHelper.WriteJsonAsync(response, new
            {
                error = message
            });
        }

    }
}
