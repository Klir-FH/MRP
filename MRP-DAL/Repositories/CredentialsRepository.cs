using BCrypt.Net;
using MRP.Models;
using MRP_DAL.Interfaces;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MRP_DAL.Repositories
{
    public class CredentialsRepository : ICredentialsRepository
    {
        private readonly NpgsqlConnection _connection;

        public CredentialsRepository(NpgsqlConnection connection)
        {
            _connection = connection;
        }

        public async Task<string?> GetHashedPasswordByUsernameAsync(string username)
        {
            const string sqlScript = @"
                SELECT c.hashed_password
                FROM credentials c
                INNER JOIN users u ON u.id = c.user_id
                WHERE u.username = @username
                LIMIT 1;";

            using var cmd = new NpgsqlCommand(sqlScript, _connection);
            cmd.Parameters.AddWithValue("username", username);

            var result = await cmd.ExecuteScalarAsync();
            return result as string;
        }

        public async Task<bool> InsertCredentialsAsync(int userId,string hashedPsw, string username)
        {
            const string sqlScript = @"
                INSERT INTO credentials (user_id, hashed_password)
                VALUES (@user_id, @hashed_password)
                RETURNING id;";

            using var cmd = new NpgsqlCommand(sqlScript, _connection);
            cmd.Parameters.AddWithValue("user_id", userId);
            cmd.Parameters.AddWithValue("hashed_password", hashedPsw);

            var result = await cmd.ExecuteScalarAsync();
            return result != null;

        }

    }
}
