using PeakOrLow.Web.Models;

namespace PeakOrLow.Web.Services;

/// <summary>Wraps all Anthropic Claude API calls.</summary>
public interface IClaudeService
{
    Task<BriefingModel?> GenerateBriefingAsync(List<NewsHeadline> headlines);
}
