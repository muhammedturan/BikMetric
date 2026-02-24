using Npgsql;
using Microsoft.Extensions.Configuration;
using BikMetric.Application.Common.Interfaces;

namespace BikMetric.Infrastructure.Data;

public static class SeedData
{
    public static async Task Seed(IConfiguration configuration, IPasswordHasher passwordHasher)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string not found.");

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        // Check if data exists
        await using var checkCmd = new NpgsqlCommand("SELECT COUNT(*) FROM users", connection);
        var count = Convert.ToInt64(await checkCmd.ExecuteScalarAsync());
        if (count > 0)
        {
            Console.WriteLine("Data already exists, skipping seed.");
            return;
        }

        var passwordHash = passwordHasher.HashPassword("admin123");
        var random = new Random(42);

        // --- Users ---
        var adminId = Guid.NewGuid();
        await Execute(connection, "INSERT INTO users (id, email, full_name, password_hash, role) VALUES (@id, @email, @name, @hash, @role)",
            new NpgsqlParameter("id", adminId),
            new NpgsqlParameter("email", "admin@bikmetric.com"),
            new NpgsqlParameter("name", "Admin User"),
            new NpgsqlParameter("hash", passwordHash),
            new NpgsqlParameter("role", "Admin"));
        Console.WriteLine("Seeded 1 user.");

        // --- News Sites ---
        var sitesData = new (string Name, string Domain, long Visitors)[]
        {
            ("Hürriyet", "hurriyet.com.tr", 45000000),
            ("Sözcü", "sozcu.com.tr", 38000000),
            ("Milliyet", "milliyet.com.tr", 30000000),
            ("Sabah", "sabah.com.tr", 35000000),
            ("Habertürk", "haberturk.com", 28000000),
            ("NTV", "ntv.com.tr", 22000000),
            ("CNN Türk", "cnnturk.com", 20000000),
            ("TRT Haber", "trthaber.com", 15000000),
            ("Cumhuriyet", "cumhuriyet.com.tr", 12000000),
            ("Yeni Şafak", "yenisafak.com", 18000000),
        };

        var siteIds = new List<Guid>();
        foreach (var (name, domain, visitors) in sitesData)
        {
            var id = Guid.NewGuid();
            siteIds.Add(id);
            await Execute(connection, "INSERT INTO news_sites (id, name, domain, monthly_visitors) VALUES (@id, @name, @domain, @visitors)",
                new NpgsqlParameter("id", id),
                new NpgsqlParameter("name", name),
                new NpgsqlParameter("domain", domain),
                new NpgsqlParameter("visitors", visitors));
        }
        Console.WriteLine($"Seeded {sitesData.Length} news sites.");

        // --- Categories ---
        var categoriesData = new (string Name, string Slug)[]
        {
            ("Gündem", "gundem"), ("Spor", "spor"), ("Ekonomi", "ekonomi"),
            ("Teknoloji", "teknoloji"), ("Magazin", "magazin"), ("Dünya", "dunya"),
            ("Sağlık", "saglik"), ("Kültür-Sanat", "kultur-sanat"),
        };

        var categoryIds = new List<Guid>();
        foreach (var (name, slug) in categoriesData)
        {
            var id = Guid.NewGuid();
            categoryIds.Add(id);
            await Execute(connection, "INSERT INTO categories (id, name, slug) VALUES (@id, @name, @slug)",
                new NpgsqlParameter("id", id),
                new NpgsqlParameter("name", name),
                new NpgsqlParameter("slug", slug));
        }
        Console.WriteLine($"Seeded {categoriesData.Length} categories.");

        // --- Authors ---
        var authorNames = new[]
        {
            "Ahmet Hakan", "Fatih Altaylı", "Yılmaz Özdil", "Abdulkadir Selvi",
            "Elif Çakır", "İsmail Saymaz", "Murat Yetkin", "Deniz Zeyrek",
            "Barış Terkoğlu", "Nagehan Alçı", "Cüneyt Özdemir", "Ruşen Çakır",
            "Murat Sabuncu", "Hakan Çelik", "Sedat Ergin", "Nuray Mert",
            "Mehmet Barlas", "Can Ataklı", "Serpil Yılmaz", "Emre Kongar",
            "Zübeyde Sarı", "Oğuz Güven", "Ferhat Ünlü", "Selçuk Tepeli",
            "Ece Üner", "Buket Aydın", "Nazlı Çelik", "Şirin Payzın",
            "Fulya Öztürk", "Dilara Gönder",
        };
        var specialties = new[] { "Gündem", "Spor", "Ekonomi", "Teknoloji", "Magazin", "Dünya", "Sağlık", "Kültür-Sanat" };

        var authorIds = new List<Guid>();
        for (int i = 0; i < authorNames.Length; i++)
        {
            var id = Guid.NewGuid();
            authorIds.Add(id);
            var siteId = siteIds[i % siteIds.Count];
            var specialty = specialties[i % specialties.Length];
            await Execute(connection, "INSERT INTO authors (id, site_id, name, specialty) VALUES (@id, @siteId, @name, @specialty)",
                new NpgsqlParameter("id", id),
                new NpgsqlParameter("siteId", siteId),
                new NpgsqlParameter("name", authorNames[i]),
                new NpgsqlParameter("specialty", specialty));
        }
        Console.WriteLine($"Seeded {authorNames.Length} authors.");

        // --- Articles (500) ---
        var titlesByCategory = new Dictionary<string, string[]>
        {
            ["Gündem"] = ["Meclis'te kritik oylama bugün gerçekleşiyor", "Yeni düzenlemeler Resmi Gazete'de yayımlandı", "Cumhurbaşkanı açıklamalarda bulundu", "Yerel seçim sonuçları kesinleşti", "Bakanlar Kurulu toplantısı sona erdi", "Anayasa Mahkemesi kararını açıkladı", "Emekli maaşlarına zam oranı belli oldu", "Yeni vergi düzenlemesi yürürlüğe girdi", "Deprem bölgesinde son durum açıklandı", "Eğitim reformu paketi meclise sunuldu"],
            ["Spor"] = ["Galatasaray derbi maçını kazandı", "Fenerbahçe'de teknik direktör değişikliği", "Beşiktaş transferde bombayı patlattı", "Milli takım Avrupa Şampiyonası'na katılıyor", "Trabzonspor liderliğe yükseldi", "Süper Lig'de haftanın sonuçları", "Basketbol milli takımı finale çıktı", "Formula 1'de Türkiye Grand Prix heyecanı", "Voleybol takımımız dünya üçüncüsü oldu", "Olimpiyat hazırlıkları tüm hızıyla sürüyor"],
            ["Ekonomi"] = ["Dolar kuru son dakika gelişmeleri", "Merkez Bankası faiz kararını açıkladı", "Borsa İstanbul'da rekor işlem hacmi", "Enflasyon rakamları beklentilerin üzerinde", "İhracat rakamları yeni rekor kırdı", "Konut fiyatlarında son durum", "Altın fiyatları haftayı nasıl kapattı", "Yeni teşvik paketi açıklandı", "İşsizlik oranı açıklandı", "Türk Lirası değer kazandı"],
            ["Teknoloji"] = ["Apple yeni iPhone modelini tanıttı", "Yapay zeka alanında çığır açan gelişme", "Türkiye'nin milli otomobili yollarda", "5G altyapısı tüm Türkiye'ye yayılıyor", "Samsung Galaxy serisinde büyük indirim", "Siber saldırılara karşı yeni önlemler", "Yerli sosyal medya uygulaması 1 milyonu aştı", "Elektrikli araç satışlarında patlama", "Blockchain teknolojisi bankacılıkta devrim", "Uzay programında yeni aşamaya geçildi"],
            ["Magazin"] = ["Ünlü çiftin sürpriz nikahı", "Dizi sezonu açılışında reytingler belli oldu", "Festival'de kırmızı halı şıklığı", "Sosyal medya fenomeni rekor kırdı", "Ünlü şarkıcının yeni albümü listeleri salladı", "Moda haftasında Türk tasarımcılar göz doldurdu", "Yeni dizi fragmanı izlenme rekoru kırdı", "Ödül töreninde gecenin yıldızları", "Ünlü oyuncunun yeni filmi vizyona girdi", "TV programında canlı yayında sürpriz"],
            ["Dünya"] = ["ABD seçimlerinde son durum", "Avrupa Birliği'nden yeni karar", "Ortadoğu'da gerilim tırmanıyor", "Rusya-Ukrayna müzakereleri devam ediyor", "Çin ekonomisinde yavaşlama sinyalleri", "İngiltere'de Brexit sonrası gelişmeler", "BM Güvenlik Konseyi acil toplandı", "Küresel iklim zirvesinden önemli kararlar", "Japonya'da deprem sonrası tsunami uyarısı", "Afrika'da insani yardım krizi büyüyor"],
            ["Sağlık"] = ["Yeni grip aşısı uygulanmaya başladı", "Kanser tedavisinde umut veren gelişme", "Sağlık Bakanlığı'ndan önemli uyarı", "Diyabet hastaları için yeni ilaç onaylandı", "Mevsimsel hastalıklara dikkat", "Hastane yatırım programı başladı", "Organ bağışında rekor artış", "Yeni pandemi önlemleri açıklandı", "Çocuk sağlığında önemli adımlar", "Sağlıklı yaşam festivali başlıyor"],
            ["Kültür-Sanat"] = ["İstanbul Film Festivali başladı", "Müzede yeni sergi: Modern sanat eserleri", "Opera sezonunun açılış gecesi", "Uluslararası kitap fuarı kapılarını açtı", "Ünlü ressamın tablosu rekor fiyata satıldı", "Tiyatro festivalinde bu hafta ne var?", "Arkeolojik kazılarda tarihi keşif", "Yeni kütüphane projesi hayata geçirildi", "Uluslararası müzik yarışmasında birincilik", "Edebiyat ödülleri sahiplerini buldu"],
        };
        var suffixes = new[] { "", " - Son Dakika", " - Detaylar", " - Güncelleme", "" };

        var today = DateTime.UtcNow.Date;
        var articleIds = new List<Guid>();
        var articleDates = new Dictionary<Guid, DateTime>();

        for (int i = 0; i < 500; i++)
        {
            var id = Guid.NewGuid();
            articleIds.Add(id);

            var siteIdx = random.Next(siteIds.Count);
            var catIdx = random.Next(categoryIds.Count);
            var authorIdx = random.Next(authorIds.Count);

            var catName = categoriesData[catIdx].Name;
            var titles = titlesByCategory[catName];
            var title = titles[random.Next(titles.Length)] + suffixes[random.Next(suffixes.Length)];

            var daysAgo = random.Next(1, 91);
            var pubDate = today.AddDays(-daysAgo).AddHours(random.Next(6, 24)).AddMinutes(random.Next(60));
            articleDates[id] = pubDate;

            var wordCount = random.Next(200, 2501);
            var hasImage = random.NextDouble() < 0.85;
            var hasVideo = random.NextDouble() < 0.25;

            await Execute(connection, "INSERT INTO articles (id, site_id, category_id, author_id, title, published_at, word_count, has_image, has_video) VALUES (@id, @siteId, @catId, @authorId, @title, @pubDate, @wordCount, @hasImage, @hasVideo)",
                new NpgsqlParameter("id", id),
                new NpgsqlParameter("siteId", siteIds[siteIdx]),
                new NpgsqlParameter("catId", categoryIds[catIdx]),
                new NpgsqlParameter("authorId", authorIds[authorIdx]),
                new NpgsqlParameter("title", title),
                new NpgsqlParameter("pubDate", pubDate),
                new NpgsqlParameter("wordCount", wordCount),
                new NpgsqlParameter("hasImage", hasImage),
                new NpgsqlParameter("hasVideo", hasVideo));
        }
        Console.WriteLine($"Seeded {articleIds.Count} articles.");

        // --- Article Stats ---
        var statCount = 0;
        foreach (var articleId in articleIds)
        {
            var pubDate = articleDates[articleId];
            var daysSince = (today - pubDate.Date).Days;
            var baseViews = random.Next(500, 50001);
            var numStatDays = Math.Min(daysSince, random.Next(3, 15));

            for (int d = 0; d < numStatDays; d++)
            {
                var statDate = pubDate.Date.AddDays(d);
                var decay = Math.Max(0.05, 1.0 / (1 + d * 0.5));

                var views = (long)(baseViews * decay * (0.7 + random.NextDouble() * 0.6));
                var clicks = (long)(views * (0.3 + random.NextDouble() * 0.5));
                var shares = (int)(views * (0.01 + random.NextDouble() * 0.07));
                var comments = (int)(views * (0.005 + random.NextDouble() * 0.025));
                var avgReadTime = Math.Round(30 + random.NextDouble() * 270, 1);

                await Execute(connection, "INSERT INTO article_stats (id, article_id, date, views, clicks, shares, comments, avg_read_time_seconds) VALUES (@id, @articleId, @date, @views, @clicks, @shares, @comments, @avgReadTime)",
                    new NpgsqlParameter("id", Guid.NewGuid()),
                    new NpgsqlParameter("articleId", articleId),
                    new NpgsqlParameter("date", DateOnly.FromDateTime(statDate)),
                    new NpgsqlParameter("views", views),
                    new NpgsqlParameter("clicks", clicks),
                    new NpgsqlParameter("shares", shares),
                    new NpgsqlParameter("comments", comments),
                    new NpgsqlParameter("avgReadTime", avgReadTime));
                statCount++;
            }
        }
        Console.WriteLine($"Seeded {statCount} article stats.");
        Console.WriteLine("Seed completed!");
    }

    private static async Task Execute(NpgsqlConnection connection, string sql, params NpgsqlParameter[] parameters)
    {
        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddRange(parameters);
        await cmd.ExecuteNonQueryAsync();
    }
}
