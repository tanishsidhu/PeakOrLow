using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PeakOrLow.Web.Services;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services.AddHttpClient("NewsApi", client =>
    client.DefaultRequestHeaders.UserAgent.ParseAdd("PeakOrLow/1.0 (+https://peakorlow.com)"));
builder.Services.AddHttpClient("Anthropic");
builder.Services.AddSingleton<ICacheService, CacheService>();
builder.Services.AddTransient<IClaudeService, ClaudeService>();
builder.Services.AddTransient<INewsService, NewsService>();
builder.Services.AddTransient<IBriefingService, BriefingService>();

builder.Build().Run();
