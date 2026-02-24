using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using BikMetric.Application.Ai.Common;
using BikMetric.Application.Common.Interfaces;
using BikMetric.Application.Query.Common;

namespace BikMetric.Infrastructure.Services;

public class OllamaService : IOllamaService
{
    private readonly HttpClient _httpClient;
    private readonly string _ollamaUrl;
    private readonly string _model;

    private const string SystemPrompt = """
        Sen bir SQL uzmanısın. PostgreSQL SQL söz dizimi kullanıyorsun.

        Mevcut veritabanı şeması:

        Tablo: news_sites (id UUID, name VARCHAR, domain VARCHAR, country VARCHAR, language VARCHAR, monthly_visitors BIGINT, created_at TIMESTAMP)
        Tablo: categories (id UUID, name VARCHAR, slug VARCHAR, created_at TIMESTAMP)
        Tablo: authors (id UUID, site_id UUID, name VARCHAR, specialty VARCHAR, created_at TIMESTAMP)
        Tablo: articles (id UUID, site_id UUID, category_id UUID, author_id UUID, title VARCHAR, published_at TIMESTAMP, word_count INTEGER, has_image BOOLEAN, has_video BOOLEAN, created_at TIMESTAMP)
        Tablo: article_stats (id UUID, article_id UUID, date DATE, views BIGINT, clicks BIGINT, shares INTEGER, comments INTEGER, avg_read_time_seconds DOUBLE PRECISION, created_at TIMESTAMP)

        İlişkiler:
        - articles.site_id → news_sites.id
        - articles.category_id → categories.id
        - articles.author_id → authors.id
        - article_stats.article_id → articles.id

        KRİTİK KURALLAR:
        1. SADECE SELECT sorguları üret.
        2. Her zaman LIMIT ekle (varsayılan 10).
        3. Fonksiyonları doğrudan yaz: SUM(views), TO_CHAR(published_at, 'YYYY-MM-DD'). ASLA prefix kullanma (F.SUM yanlış, SUM doğru).
        4. ALIAS İSİMLERİNDE SADECE İNGİLİZCE HARF, RAKAM VE ALT ÇİZGİ KULLAN. Türkçe karakter (ı, ö, ü, ç, ş, ğ, İ, Ö, Ü, Ç, Ş, Ğ) YASAK. Örnekler: AS total_views (DOĞRU), AS toplam_goruntulenme (DOĞRU), AS toplam_görüntülenme (YANLIŞ), AS baslik (DOĞRU), AS başlık (YANLIŞ).
        5. ORDER BY'da kolon numarası kullan (örn: ORDER BY 1 DESC).
        6. Subquery yerine basit JOIN kullan.
        7. Tablo alias'ları tek harf olsun: s, a, c, ns, au.

        ÖRNEK SORGULAR:

        Soru: En çok okunan 5 haber
        SQL:
        ```sql
        SELECT a.title, ns.name AS site_name, SUM(s.views) AS total_views
        FROM article_stats s
        INNER JOIN articles a ON s.article_id = a.id
        INNER JOIN news_sites ns ON a.site_id = ns.id
        GROUP BY a.title, ns.name
        ORDER BY 3 DESC
        LIMIT 5
        ```

        Soru: Kategorilere göre toplam görüntülenme
        SQL:
        ```sql
        SELECT c.name AS category_name, SUM(s.views) AS total_views
        FROM article_stats s
        INNER JOIN articles a ON s.article_id = a.id
        INNER JOIN categories c ON a.category_id = c.id
        GROUP BY c.name
        ORDER BY 2 DESC
        LIMIT 10
        ```

        Cevabını şu formatta ver:

        SQL:
        ```sql
        <sorgu>
        ```

        AÇIKLAMA:
        <kısa açıklama>
        """;

    private const string RuleGenerationPrompt = """
        Sen bir filtre kuralı uzmanısın. Kullanıcının doğal dilde yazdığı talepleri yapılandırılmış filtre kurallarına dönüştürüyorsun.

        Mevcut tablolar ve kolonları:

        Tablo: news_sites (Haber Siteleri)
          Kolonlar: name (string), domain (string), country (string), language (string), monthly_visitors (number)

        Tablo: categories (Kategoriler)
          Kolonlar: name (string), slug (string)

        Tablo: authors (Yazarlar)
          Kolonlar: name (string), specialty (string), site_name (string)

        Tablo: articles (Haberler)
          Kolonlar: title (string), published_at (date), word_count (number), has_image (number), has_video (number), site_name (string), category_name (string), author_name (string)

        Tablo: article_stats (Haber İstatistikleri)
          Kolonlar: date (date), views (number), clicks (number), shares (number), comments (number), avg_read_time_seconds (number), title (string), site_name (string), category_name (string), author_name (string)

        Kullanılabilir operatörler:
          string: equals, notEquals, contains, startsWith
          number: equals, notEquals, greaterThan, greaterThanOrEqual, lessThan, lessThanOrEqual
          date: equals, greaterThan, lessThan

        KURALLAR:
        1. Sadece yukarıdaki tablo ve kolon isimlerini kullan.
        2. Operator değerleri tam olarak yukarıdaki listeden olmalı.
        3. Tablo adı: news_sites, categories, authors, articles, article_stats
        4. Yanıtı SADECE JSON formatında ver.

        ÖRNEKLER:

        Soru: Aylık ziyaretçisi 1000000'dan fazla olan haber siteleri
        ```json
        {"table_name": "news_sites", "filters": [{"column": "monthly_visitors", "operator": "greaterThan", "value": "1000000"}], "explanation": "Aylık ziyaretçi sayısı 1 milyondan fazla olan haber siteleri filtrelendi."}
        ```

        Soru: Teknoloji kategorisindeki haberler
        ```json
        {"table_name": "articles", "filters": [{"column": "category_name", "operator": "equals", "value": "Teknoloji"}], "explanation": "Kategori adı Teknoloji olan haberler filtrelendi."}
        ```

        Cevabını SADECE JSON olarak ver:
        ```json
        <json>
        ```
        """;

    public OllamaService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClient = httpClientFactory.CreateClient("Ollama");
        _ollamaUrl = configuration["Ollama:BaseUrl"] ?? "http://localhost:11434";
        _model = configuration["Ollama:Model"] ?? "llama3.1:8b";
    }

    public async Task<SqlGenerationResult> GenerateSqlAsync(string question)
    {
        var rawText = await CallOllamaAsync(question, SystemPrompt);

        var sql = ExtractSql(rawText);
        sql = SqlSanitizer.Sanitize(sql);
        var explanation = ExtractExplanation(rawText);

        return new SqlGenerationResult
        {
            Sql = sql,
            Explanation = explanation,
            Raw = rawText,
        };
    }

    public async Task<RuleGenerationResult> GenerateRulesAsync(string question)
    {
        var rawText = await CallOllamaAsync(question, RuleGenerationPrompt);
        return ExtractJson(rawText);
    }

    private async Task<string> CallOllamaAsync(string prompt, string systemPrompt)
    {
        var requestBody = new
        {
            model = _model,
            prompt,
            system = systemPrompt,
            stream = false,
        };

        var response = await _httpClient.PostAsJsonAsync($"{_ollamaUrl}/api/generate", requestBody);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("response").GetString() ?? "";
    }

    private static string ExtractSql(string text)
    {
        // Try markdown code block
        var sqlBlockIdx = text.IndexOf("```sql", StringComparison.OrdinalIgnoreCase);
        if (sqlBlockIdx >= 0)
        {
            var start = sqlBlockIdx + 6;
            var end = text.IndexOf("```", start, StringComparison.Ordinal);
            if (end > start)
                return text[start..end].Trim();
        }

        var blockIdx = text.IndexOf("```", StringComparison.Ordinal);
        if (blockIdx >= 0)
        {
            var start = blockIdx + 3;
            var end = text.IndexOf("```", start, StringComparison.Ordinal);
            if (end > start)
                return text[start..end].Trim();
        }

        // Fallback: look for SELECT
        var lines = text.Split('\n');
        var sqlLines = new List<string>();
        var capturing = false;
        foreach (var line in lines)
        {
            if (line.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
                capturing = true;
            if (capturing)
            {
                sqlLines.Add(line);
                if (line.Contains(';'))
                    break;
            }
        }

        return sqlLines.Count > 0
            ? string.Join('\n', sqlLines).Trim().TrimEnd(';')
            : "";
    }

    private static string ExtractExplanation(string text)
    {
        string[] markers = ["AÇIKLAMA:", "Açıklama:", "açıklama:"];
        foreach (var marker in markers)
        {
            var idx = text.IndexOf(marker, StringComparison.Ordinal);
            if (idx >= 0)
                return text[(idx + marker.Length)..].Trim();
        }
        return "";
    }

    private static RuleGenerationResult ExtractJson(string text)
    {
        var jsonStr = "";

        var jsonBlockIdx = text.IndexOf("```json", StringComparison.OrdinalIgnoreCase);
        if (jsonBlockIdx >= 0)
        {
            var start = jsonBlockIdx + 7;
            var end = text.IndexOf("```", start, StringComparison.Ordinal);
            if (end > start)
                jsonStr = text[start..end].Trim();
        }
        else
        {
            var blockIdx = text.IndexOf("```", StringComparison.Ordinal);
            if (blockIdx >= 0)
            {
                var start = blockIdx + 3;
                var end = text.IndexOf("```", start, StringComparison.Ordinal);
                if (end > start)
                    jsonStr = text[start..end].Trim();
            }
            else
            {
                var braceStart = text.IndexOf('{');
                var braceEnd = text.LastIndexOf('}');
                if (braceStart >= 0 && braceEnd > braceStart)
                    jsonStr = text[braceStart..(braceEnd + 1)];
            }
        }

        if (string.IsNullOrWhiteSpace(jsonStr))
            return new RuleGenerationResult { Explanation = "JSON parse edilemedi" };

        try
        {
            var doc = JsonDocument.Parse(jsonStr);
            var root = doc.RootElement;

            var tableName = root.TryGetProperty("table_name", out var tn) ? tn.GetString() ?? "" : "";
            var explanation = root.TryGetProperty("explanation", out var ex) ? ex.GetString() ?? "" : "";

            var filters = new List<FilterRuleDto>();
            if (root.TryGetProperty("filters", out var filtersEl) && filtersEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var f in filtersEl.EnumerateArray())
                {
                    filters.Add(new FilterRuleDto
                    {
                        Column = f.TryGetProperty("column", out var c) ? c.GetString() ?? "" : "",
                        Operator = f.TryGetProperty("operator", out var o) ? o.GetString() ?? "" : "",
                        Value = f.TryGetProperty("value", out var v) ? v.GetString() ?? "" : "",
                    });
                }
            }

            return new RuleGenerationResult
            {
                TableName = tableName,
                Filters = filters,
                Explanation = explanation,
            };
        }
        catch (JsonException)
        {
            return new RuleGenerationResult { Explanation = "JSON parse edilemedi" };
        }
    }
}
