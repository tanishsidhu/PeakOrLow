using System.Text.Json;
using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PeakOrLow.Web.Models;

namespace PeakOrLow.Web.Services;

/// <summary>
/// Stores and retrieves the daily briefing via Azure Table Storage so both the
/// Web App and Azure Functions can share the same cache regardless of OS or plan.
/// Falls back to a local JSON file when storage is not configured (e.g. unit tests).
/// </summary>
public class CacheService : ICacheService
{
    private readonly TableClient? _tableClient;
    private readonly string _fallbackFilePath;
    private readonly ILogger<CacheService> _logger;

    private const string TableName = "BriefingCache";
    private const string PartitionKey = "briefing";
    private const string RowKey = "latest";

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public CacheService(IConfiguration configuration, ILogger<CacheService> logger)
    {
        _logger = logger;
        _fallbackFilePath = configuration["Briefing:CacheFilePath"] ?? "cache/briefing.json";

        var connectionString = configuration["Azure:StorageConnectionString"]
            ?? Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING")
            ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            try
            {
                var serviceClient = new TableServiceClient(connectionString);
                serviceClient.CreateTableIfNotExists(TableName);
                _tableClient = serviceClient.GetTableClient(TableName);
                _logger.LogInformation("BriefingCache table ready in Azure Table Storage");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not connect to Azure Table Storage — falling back to file cache");
            }
        }
        else
        {
            _logger.LogWarning("Azure:StorageConnectionString not set — using local file cache fallback");
            var dir = Path.GetDirectoryName(_fallbackFilePath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        }
    }

    /// <summary>Saves the briefing to Azure Table Storage (or local file as fallback).</summary>
    public async Task SaveBriefingAsync(BriefingModel briefing)
    {
        if (_tableClient is not null)
        {
            try
            {
                var entity = new BriefingCacheEntity
                {
                    PartitionKey = PartitionKey,
                    RowKey       = RowKey,
                    Content      = JsonSerializer.Serialize(briefing, _jsonOptions),
                    GeneratedAt  = briefing.GeneratedAt
                };
                await _tableClient.UpsertEntityAsync(entity);
                _logger.LogInformation("Briefing saved to Azure Table Storage (BriefingCache)");
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save briefing to Azure Table Storage");
            }
        }

        // File fallback
        try
        {
            var json = JsonSerializer.Serialize(briefing, _jsonOptions);
            await File.WriteAllTextAsync(_fallbackFilePath, json);
            _logger.LogInformation("Briefing saved to file cache at {Path}", _fallbackFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save briefing to file cache");
        }
    }

    /// <summary>Loads the most recent briefing from Azure Table Storage (or local file as fallback).</summary>
    public async Task<BriefingModel?> LoadBriefingAsync()
    {
        if (_tableClient is not null)
        {
            try
            {
                var response = await _tableClient.GetEntityAsync<BriefingCacheEntity>(PartitionKey, RowKey);
                var entity = response.Value;
                if (string.IsNullOrWhiteSpace(entity.Content)) return null;

                var briefing = JsonSerializer.Deserialize<BriefingModel>(entity.Content, _jsonOptions);
                if (briefing is not null) briefing.GeneratedAt = entity.GeneratedAt;
                return briefing;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null; // No entry yet — first run
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load briefing from Azure Table Storage");
            }
        }

        // File fallback
        try
        {
            if (!File.Exists(_fallbackFilePath)) return null;
            var json = await File.ReadAllTextAsync(_fallbackFilePath);
            return JsonSerializer.Deserialize<BriefingModel>(json, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load briefing from file cache");
            return null;
        }
    }

    /// <summary>Returns true if the briefing was generated today (UTC).</summary>
    public bool IsTodaysBriefing(BriefingModel briefing)
        => briefing.GeneratedAt.Date == DateTime.UtcNow.Date;
}

/// <summary>Azure Table Storage entity holding the latest briefing JSON.</summary>
internal class BriefingCacheEntity : ITableEntity
{
    public string PartitionKey { get; set; } = "briefing";
    public string RowKey { get; set; } = "latest";
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
}
