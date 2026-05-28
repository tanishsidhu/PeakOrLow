using PeakOrLow.Web.Models;

namespace PeakOrLow.Web.Services;

/// <summary>Orchestrates headline fetching, Claude generation, and cache persistence.</summary>
public interface IBriefingService
{
    Task<BriefingModel?> GenerateAndCacheTodaysBriefingAsync();
    Task<BriefingModel?> GetCurrentBriefingAsync();
}
