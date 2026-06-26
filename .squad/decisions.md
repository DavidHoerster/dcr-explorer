# Squad Decisions

## Active Decisions

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
