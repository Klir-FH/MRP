using Models;
using Models.DTOs;
using MRP.Models;
using MRP_DAL.Interfaces;
using Npgsql;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MRP_DAL.Repositories
{
    public class MediaEntryRepository : IMediaEntryRepository
    {
        private readonly NpgsqlConnection _connection;

        public MediaEntryRepository(NpgsqlConnection connection)
        {
            _connection = connection;
        }

        public async Task<int> CreateAsync(MediaEntryDTO media)
        {
            const string sqlScript = @"
                INSERT INTO media_entries (title, description, release_year, age_restriction, type, owner_id)
                VALUES (@title, @description, @year, @age, @type, @owner)
                RETURNING id;";

            using var cmd = new NpgsqlCommand(sqlScript, _connection);
            cmd.Parameters.AddWithValue("title", media.Title);
            cmd.Parameters.AddWithValue("description", (object?)media.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("year", (object?)media.ReleaseYear ?? DBNull.Value);
            cmd.Parameters.AddWithValue("age", (object?)media.AgeRestriction ?? DBNull.Value);
            cmd.Parameters.AddWithValue("type", (int)media.Type);
            cmd.Parameters.AddWithValue("owner", media.OwnerId!.Value);

            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }
        public async Task<bool> FavouriteAsync(int userId, int mediaId)
        {
            const string sql = @"
                INSERT INTO user_media_interactions (user_id, media_entry_id, interaction_type)
                VALUES (@userId, @mediaId, @type)
                ON CONFLICT DO NOTHING;
            ";

            using var cmd = new NpgsqlCommand(sql, _connection);
            cmd.Parameters.AddWithValue("userId", userId);
            cmd.Parameters.AddWithValue("mediaId", mediaId);
            cmd.Parameters.AddWithValue("type", (int)UserMediaInteractions.Favourite);

            return await cmd.ExecuteNonQueryAsync() > 0;
        }


        public async Task<bool> DeleteAsync(int id, int ownerId)
        {
            const string sqlScript = @"
                DELETE FROM media_entries
                WHERE id = @id AND owner_id = @owner;";

            using var cmd = new NpgsqlCommand(sqlScript, _connection);
            cmd.Parameters.AddWithValue("id", id);
            cmd.Parameters.AddWithValue("owner", ownerId);

            var affected = await cmd.ExecuteNonQueryAsync();
            return affected > 0;
        }
        public async Task<bool> UnfavouriteAsync(int userId, int mediaId)
        {
            const string sql = @"
                DELETE FROM user_media_interactions
                WHERE user_id = @user
                  AND media_entry_id = @media
                  AND interaction_type = @type;
            ";

            using var cmd = new NpgsqlCommand(sql, _connection);
            cmd.Parameters.AddWithValue("user", userId);
            cmd.Parameters.AddWithValue("media", mediaId);
            cmd.Parameters.AddWithValue("type", (int)UserMediaInteractions.Favourite);

            return await cmd.ExecuteNonQueryAsync() > 0;
        }
        public async Task<List<int>> GetFavoriteMediaIdsAsync(int userId)
        {
            const string sql = @"
                SELECT media_entry_id
                FROM user_media_interactions
                WHERE user_id = @user
                  AND interaction_type = @type
                ORDER BY media_entry_id ASC;
            ";

            using var cmd = new NpgsqlCommand(sql, _connection);
            cmd.Parameters.AddWithValue("user", userId);
            cmd.Parameters.AddWithValue("type", (int)UserMediaInteractions.Favourite);

            using var reader = await cmd.ExecuteReaderAsync();
            var list = new List<int>();

            while (await reader.ReadAsync())
                list.Add(reader.GetInt32(0));

            return list;
        }

        public async Task<MediaEntryDTO?> GetByIdAsync(int mediaId, int? userId)
        {
            const string sql = @"
                SELECT
                    m.id,
                    m.title,
                    m.description,
                    m.release_year,
                    m.age_restriction,
                    m.type,
                    m.owner_id,

                    COALESCE(AVG(r.star_value), NULL) AS avg_score,
                    COUNT(r.id)::int AS rating_count,

                    COALESCE(fav.has_interaction, FALSE) AS is_favorited,
                    COALESCE(likei.has_interaction, FALSE) AS is_liked
                FROM media_entries m
                LEFT JOIN ratings r ON r.media_entry_id = m.id

                LEFT JOIN LATERAL (
                    SELECT TRUE AS has_interaction
                    FROM user_media_interactions umi
                    WHERE umi.user_id = @userId
                      AND umi.media_entry_id = m.id
                      AND umi.interaction_type = @favType
                    LIMIT 1
                ) fav ON TRUE

                LEFT JOIN LATERAL (
                    SELECT TRUE AS has_interaction
                    FROM user_media_interactions umi
                    WHERE umi.user_id = @userId
                      AND umi.media_entry_id = m.id
                      AND umi.interaction_type = @likeType
                    LIMIT 1
                ) likei ON TRUE

                WHERE m.id = @id
                GROUP BY m.id, m.title, m.description, m.release_year, m.age_restriction, m.type, m.owner_id, fav.has_interaction, likei.has_interaction;
            ";

            using var cmd = new NpgsqlCommand(sql, _connection);
            cmd.Parameters.AddWithValue("id", mediaId);

            cmd.Parameters.AddWithValue("userId", (object?)userId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("likeType", (int)UserMediaInteractions.Like);
            cmd.Parameters.AddWithValue("favType", (int)UserMediaInteractions.Favourite);

            using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return null;

            return new MediaEntryDTO
            {
                Id = reader.GetInt32(0),
                Title = reader.GetString(1),
                Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                ReleaseYear = reader.IsDBNull(3) ? null : reader.GetString(3),
                AgeRestriction = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                Type = (MediaType)reader.GetInt32(5),
                OwnerId = reader.IsDBNull(6) ? null : reader.GetInt32(6),

                AvgScore = reader.IsDBNull(7) ? null : reader.GetDouble(7),
                RatingCount = reader.GetInt32(8),

                IsFavorited = reader.GetBoolean(9),
                IsLiked = reader.GetBoolean(10),


            };
        }
        public async Task<List<MediaEntryDTO>> SearchAsync(string? query, string? genre, int? type,
            string? year, int? ageRestriction, double? minScore, string sortBy, string sortOrder, int? userId)
        {
            // whitelist sorting
            // Fixed pre appoved collums -> Dependency injection not possible
            var sortColumn = sortBy switch
            {
                "title" => "m.title",
                "year" => "m.release_year",
                "score" => "avg_score",
                _ => "m.title"
            };

            var order = sortOrder == "desc" ? "DESC" : "ASC";

            // join ratings for avg score
            // join genres only if filter is used
            var favType = (int)UserMediaInteractions.Favourite;
            var likeType = (int)UserMediaInteractions.Like;
            

            var sql = @"
                SELECT
                    m.id, m.title, m.description, m.release_year, m.age_restriction, m.type, m.owner_id,
                    r.avg_score,
                    COALESCE(r.rating_count, 0) AS rating_count,
                    (fav.user_id IS NOT NULL) AS is_favorited,
                    (lik.user_id IS NOT NULL) AS is_liked
                FROM media_entries m
                LEFT JOIN (
                    SELECT
                        media_entry_id,
                        AVG(star_value)::float8 AS avg_score,
                        COUNT(*)::int AS rating_count
                    FROM ratings
                    GROUP BY media_entry_id
                ) r ON r.media_entry_id = m.id
                LEFT JOIN user_media_interactions fav
                    ON fav.media_entry_id = m.id
                   AND fav.user_id = @userId
                   AND fav.interaction_type = @favType
                LEFT JOIN user_media_interactions lik
                    ON lik.media_entry_id = m.id
                   AND lik.user_id = @userId
                   AND lik.interaction_type = @likeType
            ";

            if (!string.IsNullOrWhiteSpace(genre))
            {
                sql += @"
                    INNER JOIN media_entry_genres meg ON meg.media_entry_id = m.id
                    INNER JOIN genres g ON g.id = meg.genre_id
                    ";
            }

            sql += " WHERE 1=1 ";

            var parameters = new List<NpgsqlParameter>
            {
                new("userId", (object?)userId ?? DBNull.Value),
                new("likeType", (object)(int)UserMediaInteractions.Like),
                new("favType", (object)(int)UserMediaInteractions.Favourite)
            };

            if (!string.IsNullOrWhiteSpace(query))
            {
                sql += " AND m.title ILIKE @q ";
                parameters.Add(new NpgsqlParameter("q", "%" + query + "%"));
            }

            if (!string.IsNullOrWhiteSpace(genre))
            {
                sql += " AND g.name = @genre ";
                parameters.Add(new NpgsqlParameter("genre", genre));
            }

            if (type.HasValue)
            {
                sql += " AND m.type = @type ";
                parameters.Add(new NpgsqlParameter("type", type.Value));
            }

            if (!string.IsNullOrWhiteSpace(year))
            {
                sql += " AND m.release_year = @year ";
                parameters.Add(new NpgsqlParameter("year", year));
            }

            if (ageRestriction.HasValue)
            {
                sql += " AND m.age_restriction = @age ";
                parameters.Add(new NpgsqlParameter("age", ageRestriction.Value));
            }

            if (minScore.HasValue)
            {
                sql += " AND r.avg_score IS NOT NULL AND r.avg_score >= @minScore ";
                parameters.Add(new NpgsqlParameter("minScore", minScore.Value));
            }

            if (sortColumn == "avg_score")
                sql += $" ORDER BY {sortColumn} {order} NULLS LAST;";
            else
                sql += $" ORDER BY {sortColumn} {order};";

            using var cmd = new NpgsqlCommand(sql, _connection);
            foreach (var p in parameters) cmd.Parameters.Add(p);

            using var reader = await cmd.ExecuteReaderAsync();
            var allMedia = new List<MediaEntryDTO>();

            while (await reader.ReadAsync())
            {
                allMedia.Add(new MediaEntryDTO
                {
                    Id = reader.GetInt32(0),
                    Title = reader.GetString(1),
                    Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                    ReleaseYear = reader.IsDBNull(3) ? null : reader.GetString(3),
                    AgeRestriction = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                    Type = (MediaType)reader.GetInt32(5),
                    OwnerId = reader.IsDBNull(6) ? null : reader.GetInt32(6),

                    AvgScore = reader.IsDBNull(7) ? null : reader.GetDouble(7),
                    RatingCount = reader.GetInt32(8),

                    IsFavorited = reader.GetBoolean(9),
                    IsLiked = reader.GetBoolean(10)
                });
            }

            return allMedia;
        }
        public async Task<int?> GetOwnerIdByMediaIdAsync(int mediaId)
        {
            const string sql = @"SELECT owner_id FROM media_entries WHERE id=@id;";
            using var cmd = new NpgsqlCommand(sql, _connection);
            cmd.Parameters.AddWithValue("id", mediaId);

            var res = await cmd.ExecuteScalarAsync();
            return res == null || res == DBNull.Value ? null : Convert.ToInt32(res);
        }

        public async Task<List<string>> GetGenresAsync(int mediaId)
        {
            const string sql = @"
                SELECT g.name
                FROM media_entry_genres meg
                INNER JOIN genres g ON g.id = meg.genre_id
                WHERE meg.media_entry_id = @media
                ORDER BY g.name;";

            using var cmd = new NpgsqlCommand(sql, _connection);
            cmd.Parameters.AddWithValue("media", mediaId);

            using var reader = await cmd.ExecuteReaderAsync();
            var list = new List<string>();
            while (await reader.ReadAsync())
                list.Add(reader.GetString(0));

            return list;
        }

        public async Task SetGenresAsync(int mediaId, List<string> genres)
        {
            var clean = genres
                .Where(g => !string.IsNullOrWhiteSpace(g))
                .Select(g => g.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            await using var tx = await _connection.BeginTransactionAsync();

            {
                const string del = @"DELETE FROM media_entry_genres WHERE media_entry_id=@media;";
                await using var delCmd = new NpgsqlCommand(del, _connection, tx);
                delCmd.Parameters.AddWithValue("media", mediaId);
                await delCmd.ExecuteNonQueryAsync();
            }

            // add new links create genre if missing
            foreach (var name in clean)
            {
                const string upsert = @"
                    INSERT INTO genres (name)
                    VALUES (@name)
                    ON CONFLICT (name) DO UPDATE SET name = EXCLUDED.name
                    RETURNING id;";

                int genreId;
                await using (var upsertCmd = new NpgsqlCommand(upsert, _connection, tx))
                {
                    upsertCmd.Parameters.AddWithValue("name", name);
                    var idObj = await upsertCmd.ExecuteScalarAsync();
                    genreId = Convert.ToInt32(idObj);
                }

                const string link = @"
                    INSERT INTO media_entry_genres (media_entry_id, genre_id)
                    VALUES (@media, @genre)
                    ON CONFLICT DO NOTHING;";

                await using var linkCmd = new NpgsqlCommand(link, _connection, tx);
                linkCmd.Parameters.AddWithValue("media", mediaId);
                linkCmd.Parameters.AddWithValue("genre", genreId);
                await linkCmd.ExecuteNonQueryAsync();
            }

            await tx.CommitAsync();
        }

        public async Task<bool> UpdateAsync(MediaEntryDTO media)
        {
            if (media.Id == null) throw new ArgumentException("Media id is required.", nameof(media));

            const string sql = @"
                UPDATE media_entries
                SET title=@title,
                    description=@description,
                    release_year=@year,
                    age_restriction=@age,
                    type=@type
                WHERE id=@id AND owner_id=@owner;";

            using var cmd = new NpgsqlCommand(sql, _connection);
            cmd.Parameters.AddWithValue("id", media.Id);
            cmd.Parameters.AddWithValue("owner", media.OwnerId!.Value);
            cmd.Parameters.AddWithValue("title", media.Title);
            cmd.Parameters.AddWithValue("description", (object?)media.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("year", (object?)media.ReleaseYear ?? DBNull.Value);
            cmd.Parameters.AddWithValue("age", (object?)media.AgeRestriction ?? DBNull.Value);
            cmd.Parameters.AddWithValue("type", (int)media.Type);

            return await cmd.ExecuteNonQueryAsync() > 0;
        }

       
    }
}
