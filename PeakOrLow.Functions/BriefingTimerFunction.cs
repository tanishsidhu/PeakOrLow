using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using PeakOrLow.Web.Services;

namespace PeakOrLow.Functions;

/// <summary>Runs every day at 7:00 AM CET (06:00 UTC) — before European markets open at 9:00 AM CET.</summary>
public class BriefingTimerFunction
{
    private readonly IBriefingService _briefingService;
    private readonly ILogger<BriefingTimerFunction> _logger;

    public BriefingTimerFunction(IBriefingService briefingService, ILogger<BriefingTimerFunction> logger)
    {
        _briefingService = briefingService;
        _logger = logger;
    }

    [Function("DailyBriefingGenerator")]
    public async Task Run([TimerTrigger("0 0 6 * * *")] TimerInfo myTimer)
    {
        _logger.LogInformation("DailyBriefingGenerator triggered at {Time} UTC", DateTime.UtcNow);

        var briefing = await _briefingService.GenerateAndCacheTodaysBriefingAsync();

        if (briefing is not null)
            _logger.LogInformation("Briefing successfully generated for {Date}", briefing.Date);
        else
            _logger.LogError("Briefing generation failed — cache unchanged");
    }
}
