# PeakOrLow

**Markets move. Here's what it means.**

A daily AI-powered market briefing site that explains what moved today, what it means for investors, and what to expect tomorrow — in plain English.

## Architecture

- **PeakOrLow.Web** — ASP.NET Core MVC (net10.0). Serves the single-page briefing. Reads from a daily JSON cache file; never calls Claude on a page visit.
- **PeakOrLow.Functions** — Azure Timer Function (net10.0). Fires at 6:00 AM UTC (7:00 AM CET), fetches headlines from NewsAPI, calls Claude Haiku, saves `cache/briefing.json`.

## Setup (new machine)

### 1. Prerequisites

```powershell
winget install Microsoft.DotNet.SDK.10
winget install Git.Git
```

### 2. Clone

```bash
git clone https://github.com/tanishsidhu/PeakOrLow.git
cd PeakOrLow
```

### 3. Set secrets

```bash
cd PeakOrLow.Web
dotnet user-secrets set "Anthropic:ApiKey" "sk-ant-..."
dotnet user-secrets set "NewsApi:ApiKey" "your_newsapi_key"
dotnet user-secrets set "Azure:StorageConnectionString" "DefaultEndpointsProtocol=https;..."
```

### 4. Run

```bash
dotnet run --project PeakOrLow.Web
```

In Development mode the app auto-generates today's briefing on startup if the cache is missing.

## Environment Variables (Azure App Service / Function App)

| Setting Name | Description |
|---|---|
| `Anthropic__ApiKey` | Anthropic API key |
| `NewsApi__ApiKey` | NewsAPI free-tier key |
| `Azure__StorageConnectionString` | Azure Storage connection string |
| `Briefing__CacheFilePath` | `cache/briefing.json` (default) |

> Azure uses `__` (double underscore) as the separator for nested config keys.

## Waitlist emails

Stored in Azure Table Storage: **Storage Account → Tables → WaitlistEmails**

## Tech Stack

| Layer | Choice |
|---|---|
| Language | C# (.NET 10) |
| Framework | ASP.NET Core MVC |
| AI | Claude Haiku (direct HTTP — avoids SDK temperature/top_p bug) |
| News | NewsAPI (`/v2/top-headlines?category=business`) |
| Storage | Azure Table Storage |
| Scheduler | Azure Timer Function (fires 06:00 UTC daily) |
| Hosting | Azure App Service F1 Free |
