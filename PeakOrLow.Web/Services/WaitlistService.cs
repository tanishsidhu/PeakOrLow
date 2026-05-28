using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PeakOrLow.Web.Models;

namespace PeakOrLow.Web.Services;

/// <summary>Manages premium waitlist email submissions in Azure Table Storage.</summary>
public class WaitlistService : IWaitlistService
{
    private TableClient? _tableClient;
    private readonly ILogger<WaitlistService> _logger;
    private const string TableName = "WaitlistEmails";

    public WaitlistService(IConfiguration configuration, ILogger<WaitlistService> logger)
    {
        _logger = logger;
        var connectionString = configuration["Azure:StorageConnectionString"]
            ?? Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING")
            ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            try
            {
                var serviceClient = new TableServiceClient(connectionString);
                _tableClient = serviceClient.GetTableClient(TableName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Azure Table Storage could not be initialised — waitlist will be unavailable");
            }
        }
        else
        {
            _logger.LogWarning("Azure:StorageConnectionString is not configured — waitlist storage is disabled");
        }
    }

    /// <summary>Creates the table if it doesn't exist. Called on startup.</summary>
    public async Task EnsureTableExistsAsync()
    {
        if (_tableClient is null) return;
        try
        {
            await _tableClient.CreateIfNotExistsAsync();
            _logger.LogInformation("Azure Table '{Table}' is ready", TableName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not ensure Azure Table exists — waitlist submissions may fail");
        }
    }

    /// <summary>Upserts the email address. Silently succeeds on duplicates.</summary>
    public async Task AddEmailAsync(string email)
    {
        if (_tableClient is null)
        {
            _logger.LogWarning("Waitlist storage is not configured — email not saved: {Email}", email);
            throw new InvalidOperationException("Waitlist storage is not configured.");
        }

        try
        {
            var entry = new WaitlistEntry
            {
                PartitionKey = "waitlist",
                RowKey = email.ToLowerInvariant(),
                SubmittedAt = DateTime.UtcNow
            };

            await _tableClient.UpsertEntityAsync(entry);
            _logger.LogInformation("Waitlist email saved: {Email}", email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save waitlist email: {Email}", email);
            throw;
        }
    }

    /// <summary>Returns all waitlist email addresses (admin use).</summary>
    public async Task<IEnumerable<string>> GetAllEmailsAsync()
    {
        if (_tableClient is null) return Enumerable.Empty<string>();

        var emails = new List<string>();
        try
        {
            await foreach (var entity in _tableClient.QueryAsync<WaitlistEntry>(e => e.PartitionKey == "waitlist"))
                emails.Add(entity.RowKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve waitlist emails");
        }
        return emails;
    }
}
