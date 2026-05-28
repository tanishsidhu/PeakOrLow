using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PeakOrLow.Web.Models;

namespace PeakOrLow.Web.Services;

/// <summary>Fetches top market headlines from NewsAPI.</summary>
public class NewsService : INewsService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _apiKey;
    private readonly ILogger<NewsService> _logger;

    public NewsService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<NewsService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _apiKey = configuration["NewsApi:ApiKey"] ?? string.Empty;
        _logger = logger;
    }

    /// <summary>Returns the top 10 market headlines from NewsAPI. Returns an empty list on failure.</summary>
    public async Task<List<NewsHeadline>> GetTopMarketHeadlinesAsync()
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger.LogWarning("NewsApi:ApiKey is not configured — skipping headline fetch");
            return new List<NewsHeadline>();
        }

        try
        {
            // top-headlines with category=business returns focused financial news (CNBC, Bloomberg etc.)
            var url = $"https://newsapi.org/v2/top-headlines?category=business&language=en&pageSize=10&apiKey={Uri.EscapeDataString(_apiKey)}";

            var client = _httpClientFactory.CreateClient("NewsApi");
            var response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogError("NewsAPI returned {Status}: {Body}", (int)response.StatusCode, errorBody);
                return new List<NewsHeadline>();
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            var articles = doc.RootElement.GetProperty("articles");
            var headlines = new List<NewsHeadline>();

            foreach (var article in articles.EnumerateArray())
            {
                var title = article.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "";
                if (string.IsNullOrWhiteSpace(title) || title == "[Removed]") continue;

                headlines.Add(new NewsHeadline
                {
                    Title       = title,
                    Description = article.TryGetProperty("description", out var d) ? d.GetString() ?? "" : "",
                    Source      = article.TryGetProperty("source", out var s) && s.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "",
                    Url         = article.TryGetProperty("url", out var u) ? u.GetString() ?? "" : "",
                    UrlToImage  = article.TryGetProperty("urlToImage", out var img) ? img.GetString() : null,
                    PublishedAt = article.TryGetProperty("publishedAt", out var p) && DateTime.TryParse(p.GetString(), out var dt) ? dt : DateTime.UtcNow
                });
            }

            _logger.LogInformation("Fetched {Count} headlines from NewsAPI", headlines.Count);
            return headlines;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch headlines from NewsAPI");
            return new List<NewsHeadline>();
        }
    }
}
