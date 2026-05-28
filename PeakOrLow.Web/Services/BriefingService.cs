using Microsoft.Extensions.Logging;
using PeakOrLow.Web.Models;

namespace PeakOrLow.Web.Services;

/// <summary>Orchestrates headline fetching, Claude generation, and cache persistence.</summary>
public class BriefingService : IBriefingService
{
    private readonly INewsService _newsService;
    private readonly IClaudeService _claudeService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<BriefingService> _logger;

    public BriefingService(
        INewsService newsService,
        IClaudeService claudeService,
        ICacheService cacheService,
        ILogger<BriefingService> logger)
    {
        _newsService = newsService;
        _claudeService = claudeService;
        _cacheService = cacheService;
        _logger = logger;
    }

    /// <summary>Fetches headlines, calls Claude, and saves the result. Returns null if generation fails.</summary>
    public async Task<BriefingModel?> GenerateAndCacheTodaysBriefingAsync()
    {
        _logger.LogInformation("Starting daily briefing generation");

        var headlines = await _newsService.GetTopMarketHeadlinesAsync();
        if (headlines.Count == 0)
            _logger.LogWarning("No headlines fetched — Claude will generate with empty context");

        var briefing = await _claudeService.GenerateBriefingAsync(headlines);
        if (briefing is null)
        {
            _logger.LogError("Claude failed to generate a briefing");
            return null;
        }

        // Enrich Claude's headline items with the original article URLs and images by index
        for (int i = 0; i < briefing.Section1.Items.Count && i < headlines.Count; i++)
        {
            briefing.Section1.Items[i].ArticleUrl = headlines[i].Url;
            briefing.Section1.Items[i].ImageUrl   = headlines[i].UrlToImage;
        }

        await _cacheService.SaveBriefingAsync(briefing);
        _logger.LogInformation("Daily briefing generated and cached for {Date}", briefing.Date);
        return briefing;
    }

    /// <summary>Returns today's briefing from cache, or null if the cache is stale or missing.</summary>
    public async Task<BriefingModel?> GetCurrentBriefingAsync()
    {
        var briefing = await _cacheService.LoadBriefingAsync();
        if (briefing is null || !_cacheService.IsTodaysBriefing(briefing))
        {
            _logger.LogInformation("Cache miss or stale briefing — returning null");
            return null;
        }
        return briefing;
    }
}
