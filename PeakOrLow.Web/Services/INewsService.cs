using PeakOrLow.Web.Models;

namespace PeakOrLow.Web.Services;

/// <summary>Fetches top market headlines from NewsAPI.</summary>
public interface INewsService
{
    Task<List<NewsHeadline>> GetTopMarketHeadlinesAsync();
}
