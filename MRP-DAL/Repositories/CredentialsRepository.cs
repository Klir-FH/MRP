using MRP.Models;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MRP_DAL.Repositories
{
    public class CredentialsRepository
    {
        private readonly NpgsqlConnection _conn;

        public CredentialsRepository(NpgsqlConnection conn)
        {
            _conn = conn;
        }

        public Credentials? GetByUsername(string username)
        {
            using var cmd = new NpgsqlCommand(
                "SELECT id, username, hashedpassword, salt FROM credentials WHERE username = @username",
                _conn);
            cmd.Parameters.AddWithValue("username", username);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new Credentials
                {
                    Id = reader.GetInt32(0),
                    Username = reader.GetString(1),
                    HashedPassword = reader.GetString(2),
                    Salt = reader.GetString(3)
                };
            }

            return null;
        }
    }
}
