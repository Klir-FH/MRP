using MRP_DAL.Interfaces;
using MRP_Server.Http.Helpers;
using System;
using System.Net;
using System.Threading.Tasks;

namespace MRP_Server.Http.Controllers
{
    public class LeaderboardController
    {
        private readonly IUserRepository _users;

        public LeaderboardController(IUserRepository userRepository)
        {
            _users = userRepository;
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
                    case "GET" when path.Equals("/api/leaderboard", StringComparison.OrdinalIgnoreCase):
                        await HandleGetAsync(req, res);
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

        private async Task HandleGetAsync(HttpListenerRequest req, HttpListenerResponse res)
        {
            var limit = 20;
            if (int.TryParse(req.QueryString["limit"], out var l) && l > 0 && l <= 100)
                limit = l;

            var leaderboard = await _users.GetLeaderboardAsync(limit);

            res.StatusCode = 200;
            await JsonSerializationHelper.WriteJsonAsync(res, leaderboard);
        }
    }
}
