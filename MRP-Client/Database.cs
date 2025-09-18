using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MRP_Server
{
    public class Database
    {
        public const string connectionString = "Host=localhost;Port=5432;Database=mrpdb;Username=mrp_admin;Password=admin";
        public void StartConnection()
        {

            using var conn = new NpgsqlConnection(connectionString);
            conn.Open();

            using var cmd = new NpgsqlCommand("SELECT NOW()", conn);
            var result = cmd.ExecuteScalar();

            Console.WriteLine($"Postgres says: {result}");
        }
    }
}
