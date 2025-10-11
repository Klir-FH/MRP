
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
            var connection = InitializeDatabase();

            var serverAuthService = BuildServices(connection);

            var userController = new UserController(serverAuthService);
            var mediaController = new MediaController(new MediaEntryRepository(connection), serverAuthService);

            var server = new HttpServer(userController, mediaController);

            await server.StartAsync("http://localhost:8080/");

        }

        private static Npgsql.NpgsqlConnection? InitializeDatabase()
        {
            var db = new DatabaseConnection();
            db.StartConnection();

            if (db.Connection is null)
            {
                return null;
            }

            return db.Connection!;
        }

        private static ServerAuthService BuildServices(Npgsql.NpgsqlConnection connection)
        {
            var userRepo = new UserRepository(connection);
            var credentialsRepo = new CredentialsRepository(connection);
            var authService = new AuthService(credentialsRepo, userRepo);
            var tokenManager = new TokenManager();

            return new ServerAuthService(authService, userRepo, tokenManager);
        }
    }
}
