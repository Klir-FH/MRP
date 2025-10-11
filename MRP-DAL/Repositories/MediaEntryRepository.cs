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
            cmd.Parameters.AddWithValue("description", media.Description ?? "");
            cmd.Parameters.AddWithValue("year", media.ReleaseYear ?? "");
            cmd.Parameters.AddWithValue("age", media.AgeRestriction);
            cmd.Parameters.AddWithValue("type", (int)media.Type);
            cmd.Parameters.AddWithValue("owner", media.OwnerId);

            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        public async Task<List<MediaEntryDTO>> GetAllAsync()
        {
            const string sqlScript = @"
                SELECT id, title, description, release_year, age_restriction, type, owner_id
                FROM media_entries;";

            using var cmd = new NpgsqlCommand(sqlScript, _connection);
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
                    OwnerId = reader.IsDBNull(6) ? null : reader.GetInt32(6)
                });
            }
            return allMedia;
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
    }
}
