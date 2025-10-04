using Npgsql;

namespace MRP_DAL
{
    public class DatabaseConnection
    {
        public const string connectionString = "Host=localhost;Port=15432;Database=mrpdb;Username=mrp_admin;Password=admin";
        public NpgsqlConnection? conn { get; set; }
        public void StartConnection()
        {

            conn = new NpgsqlConnection(connectionString);
            try
            {
                conn.Open();
            }
            catch (TimeoutException ex)
            {
                Console.WriteLine("Connection failed... Trying again in 5000ms");
                Thread.Sleep(5000);
                conn.Open();
            }
            
            Console.WriteLine("DB started");

            Thread.Sleep(500);

            DbInitiation.CreateTables(conn);


        }
    }
}
