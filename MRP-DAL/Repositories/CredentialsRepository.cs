using BCrypt.Net;
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

        public async Task<string?> GetHashedPasswordByUsernameAsync(string username)
        {
            const string sql = @"
                SELECT c.hashed_password
                FROM credentials c
                INNER JOIN users u ON u.id = c.user_id
                WHERE u.username = @username
                LIMIT 1;";

            using var cmd = new NpgsqlCommand(sql, _conn);
            cmd.Parameters.AddWithValue("username", username);

            var result = await cmd.ExecuteScalarAsync();
            return (string)result;
        }

        public async Task<bool> InsertCredentialsAsync(string username, string hashedPsw, int userId)
        {
            const string sql = @"
                INSERT INTO credentials (user_id, hashed_password)
                VALUES (@user_id, @hashed_password)
                RETURNING id;";

            using var cmd = new NpgsqlCommand(sql, _conn);
            cmd.Parameters.AddWithValue("user_id", userId);
            cmd.Parameters.AddWithValue("hashed_password", hashedPsw);

            var result = await cmd.ExecuteScalarAsync();
            return result != null;

        }

    }
}
