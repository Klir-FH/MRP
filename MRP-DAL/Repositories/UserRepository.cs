using MRP_DAL.Interfaces;
using Npgsql;
using Npgsql.PostgresTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MRP_DAL.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly NpgsqlConnection _conn;

        public UserRepository(NpgsqlConnection conn)
        {
            _conn = conn;
        }
        public async Task<int?> GetUserIdByUsernameAsync(string username)
        {
            const string sql = "SELECT id FROM users WHERE username = @username LIMIT 1;";
            using var cmd = new NpgsqlCommand(sql, _conn);
            cmd.Parameters.AddWithValue("username", username);

            var result = await cmd.ExecuteScalarAsync();
            return result == null ? null : Convert.ToInt32(result);
        }

        public async Task<int> CreateUserAsync(string username)
        {
            const string sql = @"
                INSERT INTO users (username)
                VALUES (@username)
                RETURNING id;";

            using var cmd = new NpgsqlCommand(sql, _conn);
            cmd.Parameters.AddWithValue("username", username);

            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }
    }
}
