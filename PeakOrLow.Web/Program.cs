using PeakOrLow.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient("NewsApi", client =>
    client.DefaultRequestHeaders.UserAgent.ParseAdd("PeakOrLow/1.0 (+https://peakorlow.com)"));
builder.Services.AddHttpClient("Anthropic");
builder.Services.AddMemoryCache();

builder.Services.AddSingleton<ICacheService, CacheService>();
builder.Services.AddTransient<IClaudeService, ClaudeService>();
builder.Services.AddTransient<INewsService, NewsService>();
builder.Services.AddTransient<IBriefingService, BriefingService>();
builder.Services.AddSingleton<IWaitlistService, WaitlistService>();

var app = builder.Build();

// Ensure Azure Table Storage table exists on startup (non-fatal)
using (var scope = app.Services.CreateScope())
{
    var waitlist = scope.ServiceProvider.GetRequiredService<IWaitlistService>() as WaitlistService;
    if (waitlist is not null)
        await waitlist.EnsureTableExistsAsync();
}

// In Development: auto-generate the briefing on startup if today's cache is missing
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var cache    = scope.ServiceProvider.GetRequiredService<ICacheService>();
    var existing = await cache.LoadBriefingAsync();
    if (existing is null || !cache.IsTodaysBriefing(existing))
    {
        var briefingService = scope.ServiceProvider.GetRequiredService<IBriefingService>();
        _ = Task.Run(() => briefingService.GenerateAndCacheTodaysBriefingAsync());
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
