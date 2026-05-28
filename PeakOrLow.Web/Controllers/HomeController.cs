using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PeakOrLow.Web.Models;
using PeakOrLow.Web.Services;

namespace PeakOrLow.Web.Controllers;

/// <summary>Serves the single-page briefing view and the waitlist API endpoint.</summary>
public class HomeController : Controller
{
    private readonly IBriefingService _briefingService;
    private readonly IWaitlistService _waitlistService;
    private readonly ILogger<HomeController> _logger;

    public HomeController(IBriefingService briefingService, IWaitlistService waitlistService, ILogger<HomeController> logger)
    {
        _briefingService = briefingService;
        _waitlistService = waitlistService;
        _logger = logger;
    }

    /// <summary>Renders the main briefing page.</summary>
    public async Task<IActionResult> Index()
    {
        var briefing = await _briefingService.GetCurrentBriefingAsync();
        return View(briefing);
    }

    /// <summary>Accepts a waitlist email submission and saves it to Azure Table Storage.</summary>
    [HttpPost]
    [Route("api/waitlist")]
    public async Task<IActionResult> Waitlist([FromBody] WaitlistRequest request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(new { success = false, error = "Email is required." });

        if (!new EmailAddressAttribute().IsValid(request.Email))
            return BadRequest(new { success = false, error = "Invalid email address." });

        try
        {
            await _waitlistService.AddEmailAsync(request.Email.Trim());
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Waitlist submission failed for {Email}", request.Email);
            return StatusCode(500, new { success = false, error = "An error occurred. Please try again." });
        }
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

/// <summary>Request body for waitlist email submission.</summary>
public class WaitlistRequest
{
    public string Email { get; set; } = string.Empty;
}
