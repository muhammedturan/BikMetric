using Npgsql;
using Microsoft.Extensions.Configuration;
using BikMetric.Application.Common.Interfaces;
using BikMetric.Application.Metadata.Common;
using BikMetric.Application.Query.Common;

namespace BikMetric.Infrastructure.Services;

public class DynamicQueryService : IDynamicQueryService
{
    private readonly string _connectionString;

    private static readonly Dictionary<string, string> OperatorMap = new()
    {
        ["equals"] = "= {0}",
        ["notEquals"] = "!= {0}",
        ["greaterThan"] = "> {0}",
        ["greaterThanOrEqual"] = ">= {0}",
        ["lessThan"] = "< {0}",
        ["lessThanOrEqual"] = "<= {0}",
        ["contains"] = "ILIKE {0}",
        ["startsWith"] = "ILIKE {0}",
    };

    private static readonly Dictionary<string, TableDef> TableMetadata = new()
    {
        ["news_sites"] = new TableDef
        {
            DisplayName = "Haber Siteleri",
            BaseQuery = "SELECT * FROM news_sites",
            Columns = new Dictionary<string, ColumnDef>
            {
                ["name"] = new("Site Adı", "string", "name"),
                ["domain"] = new("Domain", "string", "domain"),
                ["country"] = new("Ülke", "string", "country"),
                ["language"] = new("Dil", "string", "language"),
                ["monthly_visitors"] = new("Aylık Ziyaretçi", "number", "monthly_visitors"),
            }
        },
        ["categories"] = new TableDef
        {
            DisplayName = "Kategoriler",
            BaseQuery = "SELECT * FROM categories",
            Columns = new Dictionary<string, ColumnDef>
            {
                ["name"] = new("Kategori Adı", "string", "name"),
                ["slug"] = new("Slug", "string", "slug"),
            }
        },
        ["authors"] = new TableDef
        {
            DisplayName = "Yazarlar",
            BaseQuery = """
                SELECT a.id, a.name, a.specialty, ns.name AS site_name
                FROM authors a
                JOIN news_sites ns ON a.site_id = ns.id
            """,
            Columns = new Dictionary<string, ColumnDef>
            {
                ["name"] = new("Yazar Adı", "string", "a.name"),
                ["specialty"] = new("Uzmanlık", "string", "a.specialty"),
                ["site_name"] = new("Haber Sitesi", "string", "ns.name"),
            }
        },
        ["articles"] = new TableDef
        {
            DisplayName = "Haberler",
            BaseQuery = """
                SELECT ar.id, ar.title, ar.published_at, ar.word_count,
                       ar.has_image, ar.has_video,
                       ns.name AS site_name, c.name AS category_name,
                       au.name AS author_name
                FROM articles ar
                JOIN news_sites ns ON ar.site_id = ns.id
                JOIN categories c ON ar.category_id = c.id
                JOIN authors au ON ar.author_id = au.id
            """,
            Columns = new Dictionary<string, ColumnDef>
            {
                ["title"] = new("Başlık", "string", "ar.title"),
                ["published_at"] = new("Yayın Tarihi", "date", "ar.published_at"),
                ["word_count"] = new("Kelime Sayısı", "number", "ar.word_count"),
                ["has_image"] = new("Görsel Var", "number", "ar.has_image"),
                ["has_video"] = new("Video Var", "number", "ar.has_video"),
                ["site_name"] = new("Haber Sitesi", "string", "ns.name"),
                ["category_name"] = new("Kategori", "string", "c.name"),
                ["author_name"] = new("Yazar", "string", "au.name"),
            }
        },
        ["article_stats"] = new TableDef
        {
            DisplayName = "Haber İstatistikleri",
            BaseQuery = """
                SELECT ast.id, ast.date, ast.views, ast.clicks, ast.shares,
                       ast.comments, ast.avg_read_time_seconds,
                       ar.title, ns.name AS site_name, c.name AS category_name,
                       au.name AS author_name
                FROM article_stats ast
                JOIN articles ar ON ast.article_id = ar.id
                JOIN news_sites ns ON ar.site_id = ns.id
                JOIN categories c ON ar.category_id = c.id
                JOIN authors au ON ar.author_id = au.id
            """,
            Columns = new Dictionary<string, ColumnDef>
            {
                ["date"] = new("Tarih", "date", "ast.date"),
                ["views"] = new("Görüntülenme", "number", "ast.views"),
                ["clicks"] = new("Tıklanma", "number", "ast.clicks"),
                ["shares"] = new("Paylaşım", "number", "ast.shares"),
                ["comments"] = new("Yorum", "number", "ast.comments"),
                ["avg_read_time_seconds"] = new("Ort. Okuma Süresi (sn)", "number", "ast.avg_read_time_seconds"),
                ["title"] = new("Haber Başlığı", "string", "ar.title"),
                ["site_name"] = new("Haber Sitesi", "string", "ns.name"),
                ["category_name"] = new("Kategori", "string", "c.name"),
                ["author_name"] = new("Yazar", "string", "au.name"),
            }
        },
    };

    public DynamicQueryService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    }

    public List<TableMetaDto> GetTablesMetadata()
    {
        var result = new List<TableMetaDto>();
        foreach (var (tableName, meta) in TableMetadata)
        {
            var columns = meta.Columns.Select(c => new ColumnMetaDto
            {
                Name = c.Key,
                DisplayName = c.Value.Display,
                Type = c.Value.Type,
            }).ToList();

            result.Add(new TableMetaDto
            {
                TableName = tableName,
                DisplayName = meta.DisplayName,
                Columns = columns,
            });
        }
        return result;
    }

    public async Task<QueryResultDto> ExecuteDynamicQueryAsync(string tableName, List<FilterRuleDto> filters)
    {
        if (!TableMetadata.TryGetValue(tableName, out var meta))
            throw new ArgumentException($"Tablo bulunamadı: {tableName}");

        var baseQuery = meta.BaseQuery.Trim();
        var whereClauses = new List<string>();
        var parameters = new List<NpgsqlParameter>();

        for (int i = 0; i < filters.Count; i++)
        {
            var filter = filters[i];

            if (!meta.Columns.TryGetValue(filter.Column, out var colDef))
                throw new ArgumentException($"Geçersiz kolon: {filter.Column}");

            if (!OperatorMap.TryGetValue(filter.Operator, out var opTemplate))
                throw new ArgumentException($"Geçersiz operatör: {filter.Operator}");

            var sqlCol = colDef.SqlColumn;
            var paramName = $"@p{i}";

            if (filter.Operator == "contains")
            {
                parameters.Add(new NpgsqlParameter($"p{i}", $"%{filter.Value}%"));
            }
            else if (filter.Operator == "startsWith")
            {
                parameters.Add(new NpgsqlParameter($"p{i}", $"{filter.Value}%"));
            }
            else if (colDef.Type == "number")
            {
                parameters.Add(new NpgsqlParameter($"p{i}", double.Parse(filter.Value)));
            }
            else
            {
                parameters.Add(new NpgsqlParameter($"p{i}", filter.Value));
            }

            var clause = $"{sqlCol} {string.Format(opTemplate, paramName)}";
            whereClauses.Add(clause);
        }

        var hasWhere = baseQuery.Contains("WHERE", StringComparison.OrdinalIgnoreCase);
        string query;
        if (whereClauses.Count > 0)
        {
            var whereStr = string.Join(" AND ", whereClauses);
            query = hasWhere
                ? $"{baseQuery} AND {whereStr}"
                : $"{baseQuery} WHERE {whereStr}";
        }
        else
        {
            query = baseQuery;
        }

        query += " LIMIT 1000";

        return await ExecuteQueryInternal(query, parameters.ToArray());
    }

    public async Task<QueryResultDto> ExecuteRawSqlAsync(string sql)
    {
        var stripped = sql.Trim().TrimEnd(';');
        if (!stripped.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Sadece SELECT sorguları çalıştırılabilir");

        return await ExecuteQueryInternal(stripped);
    }

    private async Task<QueryResultDto> ExecuteQueryInternal(string query, params NpgsqlParameter[] parameters)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var cmd = new NpgsqlCommand(query, connection);
        if (parameters.Length > 0)
            cmd.Parameters.AddRange(parameters);

        await using var reader = await cmd.ExecuteReaderAsync();

        var columns = new List<string>();
        for (int i = 0; i < reader.FieldCount; i++)
            columns.Add(reader.GetName(i));

        var rows = new List<Dictionary<string, object?>>();
        while (await reader.ReadAsync())
        {
            var row = new Dictionary<string, object?>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                row[columns[i]] = value;
            }
            rows.Add(row);
        }

        return new QueryResultDto
        {
            Columns = columns,
            Rows = rows,
            TotalCount = rows.Count,
        };
    }

    private class TableDef
    {
        public string DisplayName { get; init; } = "";
        public string BaseQuery { get; init; } = "";
        public Dictionary<string, ColumnDef> Columns { get; init; } = new();
    }

    private record ColumnDef(string Display, string Type, string SqlColumn);
}
