namespace PeakOrLow.Web.Models;

/// <summary>Daily market briefing returned by Claude.</summary>
public class BriefingModel
{
    public string Date { get; set; } = string.Empty;
    public BriefingSection1 Section1 { get; set; } = new();
    public BriefingSection2 Section2 { get; set; } = new();
    public BriefingSection3 Section3 { get; set; } = new();
    public DateTime GeneratedAt { get; set; }
}

public class BriefingSection1
{
    public string Title { get; set; } = "Today's Headlines";
    public List<HeadlineItem> Items { get; set; } = new();
}

public class HeadlineItem
{
    public string Headline { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string? ArticleUrl { get; set; }
    public string? ImageUrl { get; set; }
}

public class BriefingSection2
{
    public string Title { get; set; } = "Market Impact";
    public List<string> Paragraphs { get; set; } = new();
    public InvestmentSignal InvestmentSignal { get; set; } = new();
}

public class InvestmentSignal
{
    public string Direction { get; set; } = string.Empty;
    public string Rationale { get; set; } = string.Empty;
}

public class BriefingSection3
{
    public string Title { get; set; } = "Tomorrow's Outlook";
    public List<string> Watch { get; set; } = new();
    public string WorkplaceImpact { get; set; } = string.Empty;
    public string ComplexityRating { get; set; } = string.Empty;
    public string ComplexityReason { get; set; } = string.Empty;
}
