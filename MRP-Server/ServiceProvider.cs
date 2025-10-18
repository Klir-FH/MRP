using Jab;
using MRP_DAL;
using MRP_DAL.Interfaces;
using MRP_DAL.Repositories;
using MRP_Server.Http;
using MRP_Server.Http.Controllers;
using MRP_Server.Services;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MRP_Server
{
    [ServiceProvider]

    [Singleton<DatabaseConnection>()]
    [Singleton(typeof(NpgsqlConnection), Factory = nameof(CreateConnection))]

    [Singleton<UserRepository>()]
    [Singleton<CredentialsRepository>()]
    [Singleton<IMediaEntryRepository, MediaEntryRepository>]
    [Singleton<IUserRepository, UserRepository>]


    [Singleton<AuthService>()]
    [Singleton<TokenManager>()]
    [Singleton<ServerAuthService>()]

    [Transient<UserController>()]
    [Transient<MediaController>()]

    [Singleton<HttpServer>()] 
    public partial class ServiceProvider {
        private NpgsqlConnection CreateConnection()
        {
            var db = new DatabaseConnection();
            db.StartConnection();

            if (db.Connection is null)
                throw new InvalidOperationException("Database connection failed.");

            return db.Connection!;
        }
    }
}
