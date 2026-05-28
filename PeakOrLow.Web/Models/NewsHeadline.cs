namespace PeakOrLow.Web.Models;

/// <summary>A single headline fetched from NewsAPI.</summary>
public class NewsHeadline
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? UrlToImage { get; set; }
    public DateTime PublishedAt { get; set; }
}
