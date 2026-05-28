using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PeakOrLow.Web.Models;

namespace PeakOrLow.Web.Services;

/// <summary>Wraps all Anthropic Claude API calls via direct HTTP (bypasses SDK temperature/top_p serialisation bug).</summary>
public class ClaudeService : IClaudeService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _apiKey;
    private readonly ILogger<ClaudeService> _logger;

    private const string ModelName = "claude-haiku-4-5-20251001";
    private const int MaxTokens = 1500;
    private const string AnthropicApiUrl = "https://api.anthropic.com/v1/messages";
    private const string AnthropicVersion = "2023-06-01";

    private static readonly JsonSerializerOptions _serializeOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly JsonSerializerOptions _deserializeOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private const string SystemPrompt =
        "You are PeakOrLow, a daily market briefing assistant. Your job is to take today's " +
        "financial news headlines and produce a clear, structured, jargon-free briefing " +
        "for two audiences: (1) regular people who want to understand markets and make " +
        "basic investment decisions, and (2) professionals in financial services who want " +
        "to understand how today's market movements might affect their workload tomorrow.\n\n" +
        "Always respond in valid JSON only. No preamble, no markdown, no explanation outside the JSON.";

    public ClaudeService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<ClaudeService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _apiKey = configuration["Anthropic:ApiKey"]
            ?? Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")
            ?? string.Empty;
        _logger = logger;
    }

    /// <summary>Sends headlines to Claude and returns a parsed BriefingModel. Returns null on failure.</summary>
    public async Task<BriefingModel?> GenerateBriefingAsync(List<NewsHeadline> headlines)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger.LogError("Anthropic API key is not configured");
            return null;
        }

        try
        {
            var userPrompt = BuildUserPrompt(BuildHeadlinesList(headlines));

            var requestBody = new AnthropicRequest
            {
                Model = ModelName,
                MaxTokens = MaxTokens,
                System = SystemPrompt,
                Messages = [new AnthropicMessage { Role = "user", Content = userPrompt }]
            };

            var json = JsonSerializer.Serialize(requestBody, _serializeOptions);
            using var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

            var client = _httpClientFactory.CreateClient("Anthropic");
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, AnthropicApiUrl);
            httpRequest.Headers.Add("x-api-key", _apiKey);
            httpRequest.Headers.Add("anthropic-version", AnthropicVersion);
            httpRequest.Content = httpContent;

            var httpResponse = await client.SendAsync(httpRequest);
            var responseBody = await httpResponse.Content.ReadAsStringAsync();

            if (!httpResponse.IsSuccessStatusCode)
            {
                _logger.LogError("Claude API returned {Status}: {Body}", (int)httpResponse.StatusCode, responseBody);
                return null;
            }

            var rawContent = ExtractTextContent(responseBody);
            _logger.LogInformation("Claude responded with {Length} characters", rawContent.Length);
            return ParseBriefing(rawContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate briefing from Claude");
            return null;
        }
    }

    private static string ExtractTextContent(string responseBody)
    {
        using var doc = JsonDocument.Parse(responseBody);
        var content = doc.RootElement.GetProperty("content");
        foreach (var block in content.EnumerateArray())
        {
            if (block.TryGetProperty("type", out var type) && type.GetString() == "text"
                && block.TryGetProperty("text", out var text))
                return text.GetString() ?? string.Empty;
        }
        return string.Empty;
    }

    private static string BuildHeadlinesList(List<NewsHeadline> headlines)
    {
        if (headlines.Count == 0)
            return "(No headlines available — generate a general market overview for today.)";

        var sb = new StringBuilder();
        for (int i = 0; i < headlines.Count; i++)
        {
            var h = headlines[i];
            sb.AppendLine($"{i + 1}. {h.Title}");
            if (!string.IsNullOrWhiteSpace(h.Description))
                sb.AppendLine($"   {h.Description}");
            sb.AppendLine($"   Source: {h.Source}");
            sb.AppendLine();
        }
        return sb.ToString();
    }

    private static string BuildUserPrompt(string headlinesList)
    {
        return $$"""
            Here are today's top market headlines:
            {{headlinesList}}

            Generate a briefing in the following JSON format exactly:

            {
              "date": "YYYY-MM-DD",
              "section1": {
                "title": "Today's Headlines",
                "items": [
                  { "headline": "Short bold headline", "summary": "One sentence plain English summary" }
                ]
              },
              "section2": {
                "title": "Market Impact",
                "paragraphs": [
                  "Plain English paragraph explaining what moved and why it matters to a regular person.",
                  "A second paragraph covering sector-level impact and investment direction signal — not advice, just context."
                ],
                "investmentSignal": {
                  "direction": "Cautious | Neutral | Opportunistic",
                  "rationale": "One sentence explaining why"
                }
              },
              "section3": {
                "title": "Tomorrow's Outlook",
                "watch": ["Event or signal to watch #1", "Event or signal to watch #2"],
                "workplaceImpact": "One paragraph for professionals: what today's movements suggest about client behaviour, query volume, or operational complexity tomorrow.",
                "complexityRating": "Green | Yellow | Red",
                "complexityReason": "One sentence explaining the rating"
              }
            }
            """;
    }

    private BriefingModel? ParseBriefing(string rawJson)
    {
        try
        {
            var json = rawJson.Trim();
            if (json.StartsWith("```"))
            {
                var start = json.IndexOf('\n') + 1;
                var end = json.LastIndexOf("```");
                json = end > start ? json[start..end].Trim() : json;
            }

            var briefing = JsonSerializer.Deserialize<BriefingModel>(json, _deserializeOptions);
            if (briefing is not null)
            {
                briefing.GeneratedAt = DateTime.UtcNow;
                briefing.Date = DateTime.UtcNow.ToString("yyyy-MM-dd");
            }

            return briefing;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse Claude JSON response. Raw: {Raw}", rawJson);
            return null;
        }
    }

    // Minimal request/response shapes — only fields we actually need.
    private sealed class AnthropicRequest
    {
        public string Model { get; set; } = string.Empty;
        public int MaxTokens { get; set; }
        public string System { get; set; } = string.Empty;
        public List<AnthropicMessage> Messages { get; set; } = [];
    }

    private sealed class AnthropicMessage
    {
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }
}
