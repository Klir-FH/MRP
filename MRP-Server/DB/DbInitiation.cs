using Npgsql;

namespace MRP_Server.DB
{
    public static class DbInitiation
    {
        private static NpgsqlConnection _connection;
        public static void CreateTables(NpgsqlConnection connection)
        {
            _connection = connection;

            var sql = @"
        CREATE TABLE IF NOT EXISTS users (
            id SERIAL PRIMARY KEY
        );

        CREATE TABLE IF NOT EXISTS credentials (
            id SERIAL PRIMARY KEY,
            user_id INT UNIQUE NOT NULL,
            username VARCHAR(100) NOT NULL UNIQUE,
            hashed_password TEXT NOT NULL,
            salt TEXT NOT NULL,
            CONSTRAINT fk_credentials_user FOREIGN KEY (user_id)
                REFERENCES users (id) ON DELETE CASCADE
        );

        CREATE TABLE IF NOT EXISTS media_entries (
            id SERIAL PRIMARY KEY,
            title VARCHAR(255) NOT NULL,
            description TEXT,
            release_year VARCHAR(10),
            age_restriction INT,
            type INT NOT NULL, -- stores MediaType enum (0=Movie, 1=Serie, 2=Game)
            owner_id INT,
            CONSTRAINT fk_media_owner FOREIGN KEY (owner_id)
                REFERENCES users (id) ON DELETE SET NULL
        );

        CREATE TABLE IF NOT EXISTS genres (
            id SERIAL PRIMARY KEY,
            name VARCHAR(100) NOT NULL UNIQUE
        );

        CREATE TABLE IF NOT EXISTS media_entry_genres (
            media_entry_id INT NOT NULL,
            genre_id INT NOT NULL,
            PRIMARY KEY (media_entry_id, genre_id),
            CONSTRAINT fk_meg_media FOREIGN KEY (media_entry_id)
                REFERENCES media_entries (id) ON DELETE CASCADE,
            CONSTRAINT fk_meg_genre FOREIGN KEY (genre_id)
                REFERENCES genres (id) ON DELETE CASCADE
        );


        CREATE TABLE IF NOT EXISTS ratings (
            id SERIAL PRIMARY KEY,
            media_entry_id INT NOT NULL,
            owner_id INT NOT NULL,
            star_value INT NOT NULL CHECK (star_value BETWEEN 1 AND 5),
            comment TEXT,
            timestamp TIMESTAMPTZ NOT NULL DEFAULT NOW(),
            is_comment_visible BOOLEAN NOT NULL DEFAULT FALSE,
            CONSTRAINT fk_rating_media FOREIGN KEY (media_entry_id)
                REFERENCES media_entries (id) ON DELETE CASCADE,
            CONSTRAINT fk_rating_owner FOREIGN KEY (owner_id)
                REFERENCES users (id) ON DELETE CASCADE
        );


        CREATE TABLE IF NOT EXISTS user_media_interactions (
            user_id INT NOT NULL,
            media_entry_id INT NOT NULL,
            interaction_type INT NOT NULL, -- stores UserMediaInteractionType enum (0=Like, 1=Favourite)
            PRIMARY KEY (user_id, media_entry_id, interaction_type),
            CONSTRAINT fk_umi_user FOREIGN KEY (user_id)
                REFERENCES users (id) ON DELETE CASCADE,
            CONSTRAINT fk_umi_media FOREIGN KEY (media_entry_id)
                REFERENCES media_entries (id) ON DELETE CASCADE
        );
        ";

            using var cmd = new NpgsqlCommand(sql, _connection);
            cmd.ExecuteNonQuery();
        }
    }
}
