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
            using var checkQuery = new NpgsqlCommand(check, _connection);
            checkQuery.Parameters.AddWithValue("id", rating.MediaEntryId);

            if (await checkQuery.ExecuteScalarAsync() == null) throw new InvalidOperationException("Media entry does not exist.");

            const string sql = @"
                INSERT INTO ratings (media_entry_id, owner_id, star_value, comment)
                VALUES (@media, @owner, @stars, @comment)
                RETURNING id;";

            using var cmd = new NpgsqlCommand(sql, _connection);
            cmd.Parameters.AddWithValue("media", rating.MediaEntryId);
            cmd.Parameters.AddWithValue("owner", rating.OwnerId);
            cmd.Parameters.AddWithValue("stars", rating.StarValue);
            cmd.Parameters.AddWithValue("comment", (object?)rating.Comment ?? DBNull.Value);
            var id = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(id);
        }

        public async Task<List<Rating>> GetByMediaAsync(int mediaEntryId)
        {
            const string sql = @"
                SELECT id, media_entry_id, owner_id, star_value, comment,
                       timestamp,is_comment_visible
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
                VALUES (@user,@rating,0)
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
    }
}
