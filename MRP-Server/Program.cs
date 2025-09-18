namespace MRP_Server
{
    internal class Program
    {
        static void Main(string[] args)
        {
            HttpServer server = new HttpServer();
            server.Start();

            Database database = new();
            database.StartConnection();
            while (true)
            {
                Thread.Sleep(1000);
            }
        }
    }
}
