using Models.DTOs;
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
        private readonly NpgsqlConnection _connection;

        public UserRepository(NpgsqlConnection connection)
        {
            _connection = connection;
        }
        public async Task<int?> GetUserIdByUsernameAsync(string username)
        {
            const string sqlScript = "SELECT id FROM users WHERE username = @username LIMIT 1;";
            using var cmd = new NpgsqlCommand(sqlScript, _connection);
            cmd.Parameters.AddWithValue("username", username);

            var result = await cmd.ExecuteScalarAsync();
            return result == null ? null : Convert.ToInt32(result);
        }

        public async Task<int> CreateUserAsync(string username)
        {
            const string sqlScript = @"
                INSERT INTO users (username)
                VALUES (@username)
                RETURNING id;";

            using var cmd = new NpgsqlCommand(sqlScript, _connection);
            cmd.Parameters.AddWithValue("username", username);

            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        public async Task<UserProfileStatisticsDTO?> GetUserProfileStatsAsync(string username, CancellationToken ct = default)
        {
            const string sql = @"
WITH user_info AS (
    SELECT id, username
    FROM users
    WHERE username = @username
),
user_ratings AS (
    SELECT r.owner_id,
           COUNT(r.id) AS total_ratings,
           COALESCE(AVG(r.star_value), 0) AS average_score
    FROM ratings r
    JOIN user_info u ON r.owner_id = u.id
    GROUP BY r.owner_id
),
favorite_genre AS (
    SELECT r.owner_id,
           g.name AS genre_name,
           COUNT(*) AS genre_count
    FROM ratings r
    JOIN media_entries me ON me.id = r.media_entry_id
    JOIN media_entry_genres meg ON meg.media_entry_id = me.id
    JOIN genres g ON g.id = meg.genre_id
    JOIN user_info u ON r.owner_id = u.id
    GROUP BY r.owner_id, g.name
    ORDER BY genre_count DESC
    LIMIT 1
),
favorites_count AS (
    SELECT umi.user_id,
           COUNT(*) AS favorites_count
    FROM user_media_interactions umi
    JOIN user_info u ON umi.user_id = u.id
    WHERE umi.interaction_type = 1 -- 1 = Favourite
    GROUP BY umi.user_id
)
SELECT 
    u.username,
    COALESCE(r.total_ratings, 0)    AS total_ratings,
    COALESCE(r.average_score, 0)    AS average_score,
    fg.genre_name                   AS favorite_genre,
    COALESCE(f.favorites_count, 0)  AS favorites_count
FROM user_info u
LEFT JOIN user_ratings     r  ON r.owner_id = u.id
LEFT JOIN favorite_genre   fg ON fg.owner_id = u.id
LEFT JOIN favorites_count  f  ON f.user_id = u.id;

    ";

            using var cmd = new NpgsqlCommand(sql, _connection);
            cmd.Parameters.Add(new NpgsqlParameter("username", NpgsqlTypes.NpgsqlDbType.Varchar) { Value = username });

            using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);


            if (!await reader.ReadAsync(ct).ConfigureAwait(false))
                return null;

            return new UserProfileStatisticsDTO
            {
                Username = reader.GetString(0),
                TotalRatings = reader.GetInt32(1),
                AverageScore = reader.GetDouble(2),
                FavoriteGenre = reader.IsDBNull(3) ? null : reader.GetString(3),
                FavoritesCount = reader.GetInt32(4)
            };

        }

    }
}
