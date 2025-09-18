using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseAccess
{
    public static class DbInitiation
    {
        private static NpgsqlConnection _connection;
        public static void CreateTables(NpgsqlConnection connection)
        {
            _connection = connection;

            // change names to singular
            var sql = @"

            CREATE TABLE IF NOT EXISTS ratings (
                Id SERIAL PRIMARY KEY,
                MediaEntryId SERIAL NOT NULL,
                StarValue SERIAL NOT NULL,
                Comment TEXT,
                TimeStamp TEXT,
                IsCommentVisible BOOLEAN NOT NULL
            );
            CREATE TABLE IF NOT EXISTS ratings (
                Id SERIAL PRIMARY KEY,
                MediaEntryId SERIAL NOT NULL,
                StarValue SERIAL NOT NULL,
                Comment TEXT,
                TimeStamp TEXT,
                IsCommentVisible BOOLEAN NOT NULL
            );
            CREATE TABLE IF NOT EXISTS credentials (
                Id SERIAL PRIMARY KEY,
                Username TEXT NOT NULL,
                Password TEXT NOT NULL,
                Salt TEXT NOT NULL
            );
        ";

            var cmd = new NpgsqlCommand(sql, _connection);
            cmd.ExecuteNonQuery();
        }

    }
}
