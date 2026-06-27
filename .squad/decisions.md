# Squad Decisions

## Active Decisions

### 2026-06-26T00:00:00Z: v0.5.0 secrets hygiene (#5) + observability (#4)

**By:** Amos (backend), requested by David Hoerster
**What:** Kept ITokenAcquisition / Microsoft.Identity.Web delegated user auth (did NOT switch to DefaultAzureCredential); removed plaintext ClientSecret from appsettings.json (now empty placeholder — supply via `dotnet user-secrets`); added Application Insights via `AddApplicationInsightsTelemetry()` (connection string read from `APPLICATIONINSIGHTS_CONNECTION_STRING` env or `ApplicationInsights:ConnectionString` config, no-ops when absent); added structured `ILogger` logging at the ARM fetch seams (`GetPagedAsync`, `GetUsageAsync`) and the analysis seam (`DcrAnalysisService.AnalyzeAsync`). Documented the delegated-auth decision with an inline note in `CreateClientAsync`.
**Why:** #5 — plaintext secret risk in source-controlled config. #4 — no production diagnosability. Delegated auth is kept because switching to a managed/app identity would break per-user RBAC and the workspace picker.

### 2026-06-26T00:00:00Z: Mermaid XSS hardening (#6)

**By:** Naomi (frontend), requested by David Hoerster
**What:** Set Mermaid `securityLevel: 'strict'` and `htmlLabels: false` in `wwwroot/js/mermaid-interop.js`; documented that `host.innerHTML = svg` only ever receives Mermaid's own strict-sanitized render output (no raw user text). Extracted the weak inline `EscapeLabel` from `Components/Pages/Report.razor` into a public, unit-testable `DcrDetailBlazor.Models.MermaidLabelHelper.EscapeLabel`, hardened to: collapse all whitespace/control chars to single spaces, downgrade `"` and backtick to `'` (kills the label-wrapper breakout), strip `< > [ ] { }` markup/Mermaid-shape delimiters, cap length at 120 chars, and return `—` for empty/fully-stripped input. Report.razor now delegates to the helper (all 8 call sites unchanged — single chokepoint). Added `tests/DcrDetailBlazor.Tests/MermaidLabelHelperTests.cs` (13 cases). Build clean; 37/37 tests pass.
**Why:** #6 attacker-influenced Azure names could inject markup/diagram structure via loose securityLevel + htmlLabels + innerHTML + insufficient escaping.

### 2026-06-26T00:00:00Z: ARM nextLink pagination

**By:** Amos (backend), requested by David Hoerster
**What:** Added `NextLink` to `AzureResourceListResponse<T>` and a single `GetPagedAsync<T>` helper that loops following `nextLink` (absolute GET, same auth) until absent, with repeat-URL and 1000-page guards; all four list methods (subscriptions, workspaces, DCRs, data connectors) now return aggregated results. No interface changes.
**Why:** Issue #3 — list endpoints silently truncated at page 1.

### 2026-06-26T00:00:00Z: Test-only dependencies added for ARM pagination coverage

**By:** Alex (Tester), requested by David Hoerster
**What:** Added `NSubstitute` 5.3.0 and a `FrameworkReference` to `Microsoft.AspNetCore.App` to the test project (`tests/DcrDetailBlazor.Tests/DcrDetailBlazor.Tests.csproj`). New tests in `AzureResourceListResponseTests.cs` and `AzureArmServiceTests.cs` (fake `HttpMessageHandler` + stub `IHttpClientFactory`). Result: 21/21 passing (15 pre-existing + 6 new).
**Why:** `AzureArmService` is constructable with fakes (it injects `IHttpClientFactory`, `ITokenAcquisition`, `IConfiguration`, `ILogger`), so multi-page aggregation could be tested without any production refactor. The framework reference supplies `IConfiguration`/`IHttpClientFactory`/`ILogger` to the non-web test SDK; NSubstitute stubs the large `ITokenAcquisition` interface cleanly. No production code changed.

### 2026-06-26T00:00:00Z: DCR-Explorer release plan

**By:** Holden (Lead), requested by David Hoerster
**What:** Triaged 23 backlog issues (#2–#24) into releases — v0.5.0 (security/correctness/foundational, p1): #2, #3, #4, #5, #6 — v0.6.0 (reliability/UX hardening, p2): #7, #8, #9, #10, #11, #12, #13, #14, #15, #16, #17, #18 — backlog (low-priority, unprioritized): #19, #20, #21, #22, #23, #24.
**Why:** Sequence highest-risk security + correctness + quality-foundation work first (XSS, silent ARM truncation, secret hygiene, observability, test harness), then reliability and UX hardening, then defer nice-to-haves and the Azure.ResourceManager SDK spike.

### 2026-06-26T00:00:00Z: Added PROJECT_PLAN.md to repo

**By:** Holden (Lead), requested by David Hoerster
**What:** Created PROJECT_PLAN.md at repo root tracking issues #2–#24 grouped by release (v0.5.0 / v0.6.0 / backlog) with owner + status columns.
**Why:** Give the team a repo-tracked, human-readable mirror of the GitHub release plan.

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction
