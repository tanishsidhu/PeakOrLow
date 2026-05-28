namespace PeakOrLow.Web.Services;

/// <summary>Manages premium waitlist email submissions in Azure Table Storage.</summary>
public interface IWaitlistService
{
    Task AddEmailAsync(string email);
    Task<IEnumerable<string>> GetAllEmailsAsync();
}
