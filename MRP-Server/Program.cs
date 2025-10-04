
using MRP_DAL;
using MRP_DAL.Repositories;
using MRP_Server.Http;
using MRP_Server.Services;

namespace MRP_Server
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            DatabaseConnection database = new();
            database.StartConnection();
            var conn = database.conn;
            var userRepo = new UserRepository(conn);
            var credentialsRepo = new CredentialsRepository(conn);
            var tokenManager = new TokenManager();
            var authService = new AuthService(credentialsRepo,userRepo);

            var serverAuth = new ServerAuthService(authService, userRepo, tokenManager);

            HttpServer server = new HttpServer(serverAuth);
            await server.StartAsync("http://localhost:8080/");

        }
    }
}
