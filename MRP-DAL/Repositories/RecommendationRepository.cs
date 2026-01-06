using Models.DTOs;
using MRP.Models;
using MRP_DAL.Interfaces;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MRP_DAL.Repositories
{
    public class RecommendationRepository : IRecommendationRepository
    {
        private readonly NpgsqlConnection _connection;

        public RecommendationRepository(NpgsqlConnection connection)
        {
            _connection = connection;
        }

        public async Task<List<MediaEntryDTO>> GetRecommendationsAsync(int userId, string type, int limit = 20)
        {
            type = (type ?? "content").Trim().ToLowerInvariant();

            return type switch
            {
                "genre" => await RecommendByGenreOverlapAsync(userId, limit),
                "content" => await RecommendByContentAsync(userId, limit),
                _ => await RecommendByContentAsync(userId, limit)
            };
        }


        // overlap_count = count of genres that are also in users liked genres
        // order by overlap_count desc, then global avg_score desc, then rating_count desc
        private async Task<List<MediaEntryDTO>> RecommendByGenreOverlapAsync(int userId, int limit)
        {
            const string sql = @"
                WITH user_fav_genres AS (
                    SELECT DISTINCT meg.genre_id
                    FROM ratings r
                    JOIN media_entry_genres meg ON meg.media_entry_id = r.media_entry_id
                    WHERE r.owner_id = @userId
                      AND r.star_value >= 4
                ),
                candidates AS (
                    SELECT m.id
                    FROM media_entries m
                    WHERE NOT EXISTS (
                        SELECT 1 FROM ratings r
                        WHERE r.owner_id = @userId
                          AND r.media_entry_id = m.id
                    )
                ),
                overlap AS (
                    SELECT
                        c.id AS media_id,
                        COUNT(ufg.genre_id)::int AS overlap_count
                    FROM candidates c
                    LEFT JOIN media_entry_genres meg ON meg.media_entry_id = c.id
                    LEFT JOIN user_fav_genres ufg ON ufg.genre_id = meg.genre_id
                    GROUP BY c.id
                ),
                stats AS (
                    SELECT
                        media_entry_id,
                        AVG(star_value)::float8 AS avg_score,
                        COUNT(*)::int AS rating_count
                    FROM ratings
                    GROUP BY media_entry_id
                )
                SELECT
                    m.id, m.title, m.description, m.release_year, m.age_restriction, m.type, m.owner_id,
                    s.avg_score, COALESCE(s.rating_count, 0)::int AS rating_count,
                    COALESCE(array_remove(array_agg(DISTINCT g.name), NULL), ARRAY[]::text[]) AS genres
                FROM overlap o
                JOIN media_entries m ON m.id = o.media_id
                LEFT JOIN stats s ON s.media_entry_id = m.id
                LEFT JOIN media_entry_genres meg ON meg.media_entry_id = m.id
                LEFT JOIN genres g ON g.id = meg.genre_id
                GROUP BY
                    m.id, m.title, m.description, m.release_year, m.age_restriction, m.type, m.owner_id,
                    s.avg_score, s.rating_count,
                    o.overlap_count
                ORDER BY
                    o.overlap_count DESC,
                    s.avg_score DESC NULLS LAST,
                    s.rating_count DESC,
                    m.title ASC
                LIMIT @limit;
                ";

            await using var cmd = new NpgsqlCommand(sql, _connection);
            cmd.Parameters.AddWithValue("userId", userId);
            cmd.Parameters.AddWithValue("limit", limit);

            return await ReadMediaEntryDtoListAsync(cmd);
        }


        // find users most common type, age_restriction among ratings 
        // recommend candidates with same (type, age_restriction)
        // tie break by genre overlap, then avg_score, rating_count
        // If user has no high-rated items -> fallback to top-rated unseen media

        private async Task<List<MediaEntryDTO>> RecommendByContentAsync(int userId, int limit)
        {
            const string sql = @"
                WITH user_pref AS (
                    SELECT me.type, me.age_restriction, COUNT(*)::int AS cnt
                    FROM ratings r
                    JOIN media_entries me ON me.id = r.media_entry_id
                    WHERE r.owner_id = @userId
                      AND r.star_value >= 4
                    GROUP BY me.type, me.age_restriction
                    ORDER BY cnt DESC
                    LIMIT 1
                ),
                user_fav_genres AS (
                    SELECT DISTINCT meg.genre_id
                    FROM ratings r
                    JOIN media_entry_genres meg ON meg.media_entry_id = r.media_entry_id
                    WHERE r.owner_id = @userId
                      AND r.star_value >= 4
                ),
                candidates AS (
                    SELECT m.id
                    FROM media_entries m
                    WHERE NOT EXISTS (
                        SELECT 1 FROM ratings r
                        WHERE r.owner_id = @userId
                          AND r.media_entry_id = m.id
                    )
                    AND (
                        NOT EXISTS (SELECT 1 FROM user_pref) -- fallback: no preference
                        OR EXISTS (
                            SELECT 1 FROM user_pref p
                            WHERE p.type = m.type
                              AND p.age_restriction IS NOT DISTINCT FROM m.age_restriction
                        )
                    )
                ),
                overlap AS (
                    SELECT
                        c.id AS media_id,
                        COUNT(ufg.genre_id)::int AS overlap_count
                    FROM candidates c
                    LEFT JOIN media_entry_genres meg ON meg.media_entry_id = c.id
                    LEFT JOIN user_fav_genres ufg ON ufg.genre_id = meg.genre_id
                    GROUP BY c.id
                ),
                stats AS (
                    SELECT
                        media_entry_id,
                        AVG(star_value)::float8 AS avg_score,
                        COUNT(*)::int AS rating_count
                    FROM ratings
                    GROUP BY media_entry_id
                )
                SELECT
                    m.id, m.title, m.description, m.release_year, m.age_restriction, m.type, m.owner_id,
                    s.avg_score, COALESCE(s.rating_count, 0)::int AS rating_count,
                    COALESCE(array_remove(array_agg(DISTINCT g.name), NULL), ARRAY[]::text[]) AS genres
                FROM overlap o
                JOIN media_entries m ON m.id = o.media_id
                LEFT JOIN stats s ON s.media_entry_id = m.id
                LEFT JOIN media_entry_genres meg ON meg.media_entry_id = m.id
                LEFT JOIN genres g ON g.id = meg.genre_id
                GROUP BY
                    m.id, m.title, m.description, m.release_year, m.age_restriction, m.type, m.owner_id,
                    s.avg_score, s.rating_count,
                    o.overlap_count
                ORDER BY
                    o.overlap_count DESC,
                    s.avg_score DESC NULLS LAST,
                    s.rating_count DESC,
                    m.title ASC
                LIMIT @limit;
                ";

            await using var cmd = new NpgsqlCommand(sql, _connection);
            cmd.Parameters.AddWithValue("userId", userId);
            cmd.Parameters.AddWithValue("limit", limit);

            return await ReadMediaEntryDtoListAsync(cmd);
        }

        private async Task<List<MediaEntryDTO>> ReadMediaEntryDtoListAsync(NpgsqlCommand cmd)
        {
            await using var reader = await cmd.ExecuteReaderAsync();
            var list = new List<MediaEntryDTO>();

            while (await reader.ReadAsync())
            {
                var genres = reader.IsDBNull(9)
                    ? new List<string>()
                    : new List<string>((string[])reader.GetValue(9));

                list.Add(new MediaEntryDTO
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
                    Genres = genres,

                    IsFavorited = false,
                    IsLiked = false
                });
            }

            return list;
        }
    }
}
