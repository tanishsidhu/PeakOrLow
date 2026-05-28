using Azure;
using Azure.Data.Tables;

namespace PeakOrLow.Web.Models;

/// <summary>Azure Table Storage entity for premium waitlist email submissions.</summary>
public class WaitlistEntry : ITableEntity
{
    public string PartitionKey { get; set; } = "waitlist";
    public string RowKey { get; set; } = string.Empty; // email address
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
    public DateTime SubmittedAt { get; set; }
}
