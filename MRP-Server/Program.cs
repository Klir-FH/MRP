using MRP_Server.DB;
using MRP_Server.Http;

namespace MRP_Server
{
    internal class Program
    {
        static void Main(string[] args)
        {
            HttpServer server = new HttpServer();
            server.Start();

            DatabaseConnection database = new();
            database.StartConnection();
            while (true)
            {
                Thread.Sleep(1000);
            }
        }
    }
}
