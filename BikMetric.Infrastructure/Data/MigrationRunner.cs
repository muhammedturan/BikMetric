using Npgsql;
using Microsoft.Extensions.Configuration;

namespace BikMetric.Infrastructure.Data;

public static class MigrationRunner
{
    public static async Task RunMigrations(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string not found.");

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        var tables = new[]
        {
            """
            CREATE TABLE IF NOT EXISTS users (
                id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                email VARCHAR(255) NOT NULL,
                full_name VARCHAR(255) NOT NULL,
                password_hash VARCHAR(500) NOT NULL,
                role VARCHAR(50) NOT NULL DEFAULT 'User',
                is_active BOOLEAN NOT NULL DEFAULT TRUE,
                created_at TIMESTAMP NOT NULL DEFAULT NOW()
            )
            """,
            """
            CREATE TABLE IF NOT EXISTS news_sites (
                id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                name VARCHAR(255) NOT NULL,
                domain VARCHAR(255) NOT NULL,
                country VARCHAR(100) DEFAULT 'Türkiye',
                language VARCHAR(100) DEFAULT 'Türkçe',
                monthly_visitors BIGINT NOT NULL DEFAULT 0,
                created_at TIMESTAMP NOT NULL DEFAULT NOW()
            )
            """,
            """
            CREATE TABLE IF NOT EXISTS categories (
                id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                name VARCHAR(255) NOT NULL,
                slug VARCHAR(255) NOT NULL,
                created_at TIMESTAMP NOT NULL DEFAULT NOW()
            )
            """,
            """
            CREATE TABLE IF NOT EXISTS authors (
                id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                site_id UUID REFERENCES news_sites(id),
                name VARCHAR(255) NOT NULL,
                specialty VARCHAR(255) NOT NULL,
                created_at TIMESTAMP NOT NULL DEFAULT NOW()
            )
            """,
            """
            CREATE TABLE IF NOT EXISTS articles (
                id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                site_id UUID REFERENCES news_sites(id),
                category_id UUID REFERENCES categories(id),
                author_id UUID REFERENCES authors(id),
                title VARCHAR(500) NOT NULL,
                published_at TIMESTAMP NOT NULL,
                word_count INTEGER NOT NULL DEFAULT 0,
                has_image BOOLEAN NOT NULL DEFAULT TRUE,
                has_video BOOLEAN NOT NULL DEFAULT FALSE,
                created_at TIMESTAMP NOT NULL DEFAULT NOW()
            )
            """,
            """
            CREATE TABLE IF NOT EXISTS article_stats (
                id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                article_id UUID REFERENCES articles(id),
                date DATE NOT NULL,
                views BIGINT NOT NULL DEFAULT 0,
                clicks BIGINT NOT NULL DEFAULT 0,
                shares INTEGER NOT NULL DEFAULT 0,
                comments INTEGER NOT NULL DEFAULT 0,
                avg_read_time_seconds DOUBLE PRECISION NOT NULL DEFAULT 0,
                created_at TIMESTAMP NOT NULL DEFAULT NOW()
            )
            """,
            // Indexes
            "CREATE INDEX IF NOT EXISTS idx_articles_site_id ON articles(site_id)",
            "CREATE INDEX IF NOT EXISTS idx_articles_category_id ON articles(category_id)",
            "CREATE INDEX IF NOT EXISTS idx_articles_author_id ON articles(author_id)",
            "CREATE INDEX IF NOT EXISTS idx_article_stats_article_id ON article_stats(article_id)",
            "CREATE INDEX IF NOT EXISTS idx_article_stats_date ON article_stats(date)",
        };

        foreach (var ddl in tables)
        {
            await using var cmd = new NpgsqlCommand(ddl, connection);
            await cmd.ExecuteNonQueryAsync();
        }

        Console.WriteLine("Migrations completed.");
    }
}
