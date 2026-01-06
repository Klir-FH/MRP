using Models;
using Models.DTOs;
using MRP.Models;
using MRP_DAL.Interfaces;
using Npgsql;
using System;
using System.Threading;
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
            const string sql = "SELECT id FROM users WHERE username = @username LIMIT 1;";
            await using var cmd = new NpgsqlCommand(sql, _connection);
            cmd.Parameters.AddWithValue("username", username);

            var result = await cmd.ExecuteScalarAsync();
            return result == null || result == DBNull.Value ? null : Convert.ToInt32(result);
        }

        public async Task<int> CreateUserAsync(string username)
        {
            const string sql = @"
                INSERT INTO users (username)
                VALUES (@username)
                RETURNING id;";

            await using var cmd = new NpgsqlCommand(sql, _connection);
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
                           COUNT(r.id)::int AS total_ratings,
                           COALESCE(AVG(r.star_value), 0)::float8 AS average_score
                    FROM ratings r
                    JOIN user_info u ON r.owner_id = u.id
                    GROUP BY r.owner_id
                ),
                favorite_genre AS (
                    SELECT g.name AS genre_name,
                           COUNT(*)::int AS genre_count
                    FROM ratings r
                    JOIN user_info u ON r.owner_id = u.id
                    JOIN media_entries me ON me.id = r.media_entry_id
                    JOIN media_entry_genres meg ON meg.media_entry_id = me.id
                    JOIN genres g ON g.id = meg.genre_id
                    GROUP BY g.name
                    ORDER BY genre_count DESC, g.name ASC
                    LIMIT 1
                ),
                favorites_count AS (
                    SELECT COUNT(*)::int AS favorites_count
                    FROM user_media_interactions umi
                    JOIN user_info u ON umi.user_id = u.id
                    WHERE umi.interaction_type = @favType
                )
                SELECT 
                    u.username,
                    COALESCE(r.total_ratings, 0) AS total_ratings,
                    COALESCE(r.average_score, 0) AS average_score,
                    fg.genre_name AS favorite_genre,
                    COALESCE(f.favorites_count, 0) AS favorites_count
                FROM user_info u
                LEFT JOIN user_ratings r  ON r.owner_id = u.id
                LEFT JOIN favorite_genre fg ON TRUE
                LEFT JOIN favorites_count f  ON TRUE;
                ";

            await using var cmd = new NpgsqlCommand(sql, _connection);
            cmd.Parameters.AddWithValue("username", username);
            cmd.Parameters.AddWithValue("favType", (int)UserMediaInteractions.Favourite);

            await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
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

        public async Task<(string? Email, Genre? FavoriteGenre)?> GetProfileAsync(int userId)
        {
            const string sql = @"
                SELECT
                    u.email,
                    g.id,
                    g.name
                FROM users u
                LEFT JOIN genres g ON g.id = u.favorite_genre_id
                WHERE u.id = @id
                LIMIT 1;
            ";

            await using var cmd = new NpgsqlCommand(sql, _connection);
            cmd.Parameters.AddWithValue("id", userId);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return null;

            string? email = reader.IsDBNull(0) ? null : reader.GetString(0);

            Genre? fav = null;
            if (!reader.IsDBNull(1))
            {
                fav = new Genre
                {
                    Id = reader.GetInt32(1),
                    Name = reader.IsDBNull(2) ? "" : reader.GetString(2)
                };
            }

            return (email, fav);
        }

        public async Task<bool> UpdateProfileAsync(int userId, string? email, string favoriteGenreId)
        {
            const string sql = @"
                UPDATE users
                SET email = @email,
                    favorite_genre_id = @genreId
                WHERE id = @id;
            ";

            await using var cmd = new NpgsqlCommand(sql, _connection);
            cmd.Parameters.AddWithValue("id", userId);
            cmd.Parameters.AddWithValue("email", (object?)email ?? DBNull.Value);
            cmd.Parameters.AddWithValue("genreId", (object?)favoriteGenreId ?? DBNull.Value);

            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public async Task<string?> GetUsernameByIdAsync(int userId)
        {
            const string sql = @"
                SELECT username
                FROM users
                WHERE id = @id
                LIMIT 1;
            ";

            await using var cmd = new NpgsqlCommand(sql, _connection);
            cmd.Parameters.AddWithValue("id", userId);

            var result = await cmd.ExecuteScalarAsync();
            return result == null || result == DBNull.Value ? null : result.ToString();
        }
        public async Task<object> GetLeaderboardAsync(int limit)
        {
            const string sql = @"
                SELECT
                    u.id,
                    u.username,
                    COUNT(r.id)::int AS rating_count
                FROM users u
                LEFT JOIN ratings r ON r.owner_id = u.id
                GROUP BY u.id, u.username
                ORDER BY rating_count DESC, u.username ASC
                LIMIT @limit;
            ";

            using var cmd = new NpgsqlCommand(sql, _connection);
            cmd.Parameters.AddWithValue("limit", limit);

            using var reader = await cmd.ExecuteReaderAsync();
            var list = new List<object>();

            while (await reader.ReadAsync())
            {
                list.Add(new
                {
                    userId = reader.GetInt32(0),
                    username = reader.GetString(1),
                    ratingCount = reader.GetInt32(2)
                });
            }

            return list;
        }

    }
}
