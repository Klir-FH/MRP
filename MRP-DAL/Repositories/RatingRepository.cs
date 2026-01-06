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
    public class RatingRepository : IRatingRepository
    {
        private readonly NpgsqlConnection _connection;
        public RatingRepository(NpgsqlConnection conn) => _connection = conn;

        public async Task<int> CreateAsync(Rating rating)
        {
            const string check = "SELECT 1 FROM media_entries WHERE id=@id;";
            using (var checkQuery = new NpgsqlCommand(check, _connection))
            {
                checkQuery.Parameters.AddWithValue("id", rating.MediaEntryId);
                if (await checkQuery.ExecuteScalarAsync() == null)
                {
                    throw new InvalidOperationException("Media entry does not exist.");
                }
            }

            const string sql = @"
                INSERT INTO ratings (media_entry_id, owner_id, star_value, comment, is_comment_visible)
                VALUES (@media, @owner, @stars, @comment, FALSE)
                RETURNING id;";

            try
            {
                using var cmd = new NpgsqlCommand(sql, _connection);
                cmd.Parameters.AddWithValue("media", rating.MediaEntryId);
                cmd.Parameters.AddWithValue("owner", rating.OwnerId);
                cmd.Parameters.AddWithValue("stars", rating.StarValue);
                cmd.Parameters.AddWithValue("comment", (object?)rating.Comment ?? DBNull.Value);
                var id = await cmd.ExecuteScalarAsync();
                return Convert.ToInt32(id);
            }
            catch (PostgresException ex) when (ex.SqlState == "23505")//unique constraint violation
            {
                throw new InvalidOperationException("User already rated this media entry.");
            }
        }


        public async Task<List<Rating>> GetByMediaAsync(int mediaEntryId)
        {
            const string sql =  @"
                SELECT id, media_entry_id, owner_id, star_value,
                    CASE WHEN is_comment_visible THEN comment ELSE NULL END AS comment,
                    timestamp, is_comment_visible
                FROM ratings
                WHERE media_entry_id=@media;";

            using var cmd = new NpgsqlCommand(sql, _connection);
            cmd.Parameters.AddWithValue("media", mediaEntryId);

            using var reader = await cmd.ExecuteReaderAsync();
            var list = new List<Rating>();
            while (await reader.ReadAsync())
            {
                list.Add(new Rating
                {
                    Id = reader.GetInt32(0),
                    MediaEntryId = reader.GetInt32(1),
                    OwnerId = reader.GetInt32(2),
                    StarValue = reader.GetInt32(3),
                    Comment = reader.IsDBNull(4) ? null : reader.GetString(4),
                    Timestamp = reader.GetDateTime(5),
                    IsCommentVisible = reader.GetBoolean(6)
                });
            }
            return list;
        }

        public async Task<bool> ConfirmCommentAsync(int ratingId, int ownerId)
        {
            const string sql = @"
                UPDATE ratings
                SET is_comment_visible=TRUE
                WHERE id=@rating AND owner_id=@owner;";
            using var cmd = new NpgsqlCommand(sql, _connection);
            cmd.Parameters.AddWithValue("rating", ratingId);
            cmd.Parameters.AddWithValue("owner", ownerId);
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> LikeRatingAsync(int ratingId, int userId)
        {
            const string sql = @"
                INSERT INTO user_rating_interactions (user_id, rating_id, interaction_type)
                SELECT @user, r.id, 0
                FROM ratings r
                WHERE r.id=@rating
                    AND r.owner_id<>@user
                ON CONFLICT DO NOTHING;";
            using var cmd = new NpgsqlCommand(sql, _connection);
            cmd.Parameters.AddWithValue("user", userId);
            cmd.Parameters.AddWithValue("rating", ratingId);
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> UnlikeRatingAsync(int ratingId, int userId)
        {
            const string sql = @"
                DELETE FROM user_rating_interactions
                WHERE user_id=@user AND rating_id=@rating AND interaction_type=0;";
            using var cmd = new NpgsqlCommand(sql, _connection);
            cmd.Parameters.AddWithValue("user", userId);
            cmd.Parameters.AddWithValue("rating", ratingId);
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public async Task<int> GetLikeCountAsync(int ratingId)
        {
            const string sql = @"
                SELECT COUNT(*) FROM user_rating_interactions
                WHERE rating_id=@rating AND interaction_type=0;";
            using var cmd = new NpgsqlCommand(sql, _connection);
            cmd.Parameters.AddWithValue("rating", ratingId);
            var count = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(count);
        }

        public async Task<bool> UpdateAsync(int ratingId, int ownerId, int stars, string? comment)
        {
            if (stars < 1 || stars > 5)
                throw new ArgumentOutOfRangeException(nameof(stars), "StarValue must be between 1 and 5.");

            const string sql = @"
                UPDATE ratings
                SET star_value=@stars,
                    comment=@comment,
                    is_comment_visible=FALSE
                WHERE id=@id AND owner_id=@owner;";

            using var cmd = new NpgsqlCommand(sql, _connection);
            cmd.Parameters.AddWithValue("id", ratingId);
            cmd.Parameters.AddWithValue("owner", ownerId);
            cmd.Parameters.AddWithValue("stars", stars);
            cmd.Parameters.AddWithValue("comment", (object?)comment ?? DBNull.Value);

            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> DeleteAsync(int ratingId, int ownerId)
        {
            const string sql = @"DELETE FROM ratings WHERE id=@id AND owner_id=@owner;";
            using var cmd = new NpgsqlCommand(sql, _connection);
            cmd.Parameters.AddWithValue("id", ratingId);
            cmd.Parameters.AddWithValue("owner", ownerId);
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public async Task<object> GetByUserAsync(int userId)
        {
            const string sql = @"
                SELECT
                    r.id,
                    r.media_entry_id,
                    r.star_value,
                    r.comment,
                    r.is_comment_visible,
                    r.timestamp
                FROM ratings r
                WHERE r.owner_id = @userId
                ORDER BY r.timestamp DESC, r.id DESC;
            ";

            using var cmd = new NpgsqlCommand(sql, _connection);
            cmd.Parameters.AddWithValue("userId", userId);

            using var reader = await cmd.ExecuteReaderAsync();

            var list = new List<object>();

            while (await reader.ReadAsync())
            {
                list.Add(new
                {
                    id = reader.GetInt32(0),
                    mediaId = reader.GetInt32(1),
                    stars = reader.GetInt32(2),
                    comment = reader.IsDBNull(3) ? null : reader.GetString(3),
                    isVisible = reader.GetBoolean(4),
                    createdAt = reader.IsDBNull(5) ? (DateTime?)null : reader.GetDateTime(5)
                });
            }

            return list;
        }

    }
}
