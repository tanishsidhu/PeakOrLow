using PeakOrLow.Web.Models;

namespace PeakOrLow.Web.Services;

/// <summary>Reads and writes the daily briefing to/from a flat JSON file on disk.</summary>
public interface ICacheService
{
    Task SaveBriefingAsync(BriefingModel briefing);
    Task<BriefingModel?> LoadBriefingAsync();
    bool IsTodaysBriefing(BriefingModel briefing);
}
