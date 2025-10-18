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
        public static (bool IsValid, string? Username) ValidateAndExtractToken(HttpListenerRequest req, HttpListenerResponse res, ServerAuthService authService)
        {
            string? header = req.Headers["Authorization"];
            if (header == null || !header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                res.StatusCode = 401;
                JsonSerializationHelper.WriteJsonAsync(res, new { error = "Missing or invalid Authorization header" }).Wait();
                return (false, null);
            }

            string token = header.Substring("Bearer ".Length).Trim();

            if (!authService.ValidateToken(token))
            {
                res.StatusCode = 403;
                JsonSerializationHelper.WriteJsonAsync(res, new { error = "Invalid or expired token" }).Wait();
                return (false, null);
            }

            string? username = authService.GetTokenSubject(token);
            return (true, username);
        }
    }
}
