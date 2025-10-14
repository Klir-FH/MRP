
using MRP_DAL;
using MRP_DAL.Repositories;
using MRP_Server.Http;
using MRP_Server.Http.Controllers;
using MRP_Server.Services;
using Npgsql;

namespace MRP_Server
{
    internal class Program 
    {
        static async Task Main(string[] args)
        {
            var provider = new ServiceProvider();

            var server = provider.GetService<HttpServer>();
            await server.StartAsync("http://localhost:8080/");
        }
    }
}
