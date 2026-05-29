# PeakOrLow

**Markets move. Here's what it means.**

A daily AI-powered market briefing site — plain English explanations of what moved, what it means for investors, and what to watch. Built with ASP.NET Core + Claude Haiku.

**Live site:** https://peakorlow.azurewebsites.net

---

## Architecture

- **PeakOrLow.Web** — ASP.NET Core MVC (net10.0). Serves the single-page briefing. Reads the daily briefing from Azure Table Storage on every page load.
- **PeakOrLow.Functions** — Azure Timer Function (net10.0). Fires at **6:00 AM UTC (7:00 AM CET)** daily, fetches headlines from NewsAPI, calls Claude Haiku, saves the briefing to Azure Table Storage.

### How the briefing cache works

The briefing is stored in **Azure Table Storage** (`BriefingCache` table, `peakorlowstorage` account) — not on the filesystem. This allows both the Web App (Linux) and the Functions App (Windows) to share the same cache. The `CacheService` falls back to a local JSON file only when storage is not configured (useful for running without an Azure account).

---

## New laptop setup — complete runbook

### 1. Prerequisites

```powershell
winget install Microsoft.DotNet.SDK.10
winget install Git.Git
winget install Microsoft.AzureCLI   # optional, only needed for Azure deployments
```

Restart your terminal after installation.

### 2. Clone

```bash
git clone https://github.com/tanishsidhu/PeakOrLow.git
cd PeakOrLow
```

### 3. Set secrets (never stored in git)

```bash
cd PeakOrLow.Web
dotnet user-secrets set "Anthropic:ApiKey"              "sk-ant-..."
dotnet user-secrets set "NewsApi:ApiKey"                "..."
dotnet user-secrets set "Azure:StorageConnectionString" "DefaultEndpointsProtocol=https;AccountName=peakorlowstorage;..."
```

All three values are available in your Azure Portal or from a secure password manager.

### 4. Run

```bash
dotnet run --project PeakOrLow.Web
```

On first start the app auto-generates today's briefing in the background (reads headlines from NewsAPI, calls Claude, saves to Azure Table Storage). Open http://localhost:5215 — the briefing appears within ~15 seconds.

---

## Azure resources (already created — do not recreate)

| Resource | Name | SKU |
|---|---|---|
| App Service Plan | `peakorlow-plan` | F1 Free, Linux, West Europe |
| Web App | `peakorlow` | peakorlow.azurewebsites.net |
| Functions App | `peakorlow-functions` | Consumption, Windows, West Europe |
| Storage Account | `peakorlowstorage` | LRS, West Europe |

### Azure App Settings (both Web App and Functions App)

Set under **Configuration → Application Settings** using `__` as the separator:

| Setting | Description |
|---|---|
| `Anthropic__ApiKey` | Anthropic API key |
| `NewsApi__ApiKey` | NewsAPI free-tier key |
| `Azure__StorageConnectionString` | Storage account connection string |

### CI/CD

Push to `main` → GitHub Actions builds and deploys `PeakOrLow.Web` automatically.
Workflow file: `.github/workflows/main_peakorlow.yml`
Required GitHub secret: `AZURE_CREDENTIALS` (service principal JSON).

---

## Waitlist emails

Stored in Azure Table Storage: **Storage Account → Tables → WaitlistEmails**

---

## Tech stack

| Layer | Choice |
|---|---|
| Language | C# (.NET 10) |
| Framework | ASP.NET Core MVC |
| AI | Claude Haiku — direct HTTP (avoids SDK `temperature`/`top_p` serialisation bug) |
| News | NewsAPI `/v2/top-headlines?category=business` |
| Cache | Azure Table Storage (`BriefingCache` table) |
| Waitlist | Azure Table Storage (`WaitlistEmails` table) |
| Scheduler | Azure Timer Function — cron `0 0 6 * * *` (6 AM UTC = 7 AM CET) |
| Hosting | Azure App Service F1 Free (Linux) |
