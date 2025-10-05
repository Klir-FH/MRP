
using MRP_DAL;
using MRP_DAL.Repositories;
using MRP_Server.Http;
using MRP_Server.Http.Controllers;
using MRP_Server.Services;

namespace MRP_Server
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            DatabaseConnection database = new();
            database.StartConnection();
            var connection = database.Connection;

            var userRepo = new UserRepository(connection);
            var credentialsRepo = new CredentialsRepository(connection);
            var mediaRepo = new MediaEntryRepository(connection);

            var Auth = new AuthService(credentialsRepo, userRepo);
            var tokenManager = new TokenManager();
            var serverAuth = new ServerAuthService(Auth, userRepo, tokenManager);

            var userController = new UserController(serverAuth);
            var mediaController = new MediaController(mediaRepo,serverAuth);

            var server = new HttpServer(userController, mediaController);
            await server.StartAsync("http://localhost:8080/");

        }
    }
}
