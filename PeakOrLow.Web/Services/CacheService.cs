using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PeakOrLow.Web.Models;

namespace PeakOrLow.Web.Services;

/// <summary>Reads and writes the daily briefing to a flat JSON file on disk.</summary>
public class CacheService : ICacheService
{
    private readonly string _cacheFilePath;
    private readonly ILogger<CacheService> _logger;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public CacheService(IConfiguration configuration, ILogger<CacheService> logger)
    {
        _logger = logger;
        _cacheFilePath = configuration["Briefing:CacheFilePath"] ?? "cache/briefing.json";

        var dir = Path.GetDirectoryName(_cacheFilePath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
    }

    /// <summary>Serialises the briefing model to JSON and writes it to disk.</summary>
    public async Task SaveBriefingAsync(BriefingModel briefing)
    {
        try
        {
            var json = JsonSerializer.Serialize(briefing, _jsonOptions);
            await File.WriteAllTextAsync(_cacheFilePath, json);
            _logger.LogInformation("Briefing saved to cache at {Path}", _cacheFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save briefing to cache");
            throw;
        }
    }

    /// <summary>Loads and deserialises the briefing from disk. Returns null if the file is missing or corrupt.</summary>
    public async Task<BriefingModel?> LoadBriefingAsync()
    {
        try
        {
            if (!File.Exists(_cacheFilePath))
                return null;

            var json = await File.ReadAllTextAsync(_cacheFilePath);
            return JsonSerializer.Deserialize<BriefingModel>(json, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load briefing from cache");
            return null;
        }
    }

    /// <summary>Returns true if the briefing was generated today (UTC), using GeneratedAt rather than Claude's date field.</summary>
    public bool IsTodaysBriefing(BriefingModel briefing)
        => briefing.GeneratedAt.Date == DateTime.UtcNow.Date;
}
