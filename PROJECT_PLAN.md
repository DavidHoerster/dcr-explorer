# Project Plan — dcr-detail-blazor

**dcr-detail-blazor** is a .NET 9 / Blazor Server app that explores Azure Data
Collection Rules (DCRs), maps DCRs → tables as Mermaid diagrams, and correlates
Microsoft Sentinel data connectors.

This plan tracks the **post-code-review remediation backlog** (GitHub issues
#2–#24) and groups it into releases. GitHub issues are the source of truth; this
document mirrors the release grouping so the team has a single, human-readable
roadmap.

---

## Release roadmap

### v0.5.0 — Security, correctness & foundation (p1)

| Issue | Title | Type | Priority | Owner | Status |
|-------|-------|------|----------|-------|--------|
| [#2](https://github.com/DavidHoerster/dcr-explorer/issues/2) | No automated test project | Testing | p1 | Holden / Alex | Done |
| [#3](https://github.com/DavidHoerster/dcr-explorer/issues/3) | ARM list responses ignore nextLink (silent truncation) | Correctness | p1 | Amos | Done |
| [#4](https://github.com/DavidHoerster/dcr-explorer/issues/4) | No observability (App Insights / structured logging) | Observability | p1 | Holden | Done |
| [#5](https://github.com/DavidHoerster/dcr-explorer/issues/5) | Secrets/config hygiene: appsettings not gitignored; plaintext ClientSecret | Security | p1 | Holden | Done |
| [#6](https://github.com/DavidHoerster/dcr-explorer/issues/6) | Mermaid diagram XSS risk (loose securityLevel + innerHTML) | Security | p1 | Naomi | Done |

### v0.6.0 — Reliability & UX hardening (p2)

| Issue | Title | Type | Priority | Owner | Status |
|-------|-------|------|----------|-------|--------|
| [#7](https://github.com/DavidHoerster/dcr-explorer/issues/7) | No retry/throttling (429/5xx) policy on ARM calls | Reliability | p2 | Amos | Not started |
| [#8](https://github.com/DavidHoerster/dcr-explorer/issues/8) | No explicit HttpClient timeout | Reliability | p2 | Amos | Not started |
| [#9](https://github.com/DavidHoerster/dcr-explorer/issues/9) | No caching of subscriptions/workspaces/DCRs | Performance | p2 | Amos | Not started |
| [#10](https://github.com/DavidHoerster/dcr-explorer/issues/10) | AnalyzeAsync synchronous internally, ignores cancellation | Reliability | p2 | Amos | Not started |
| [#11](https://github.com/DavidHoerster/dcr-explorer/issues/11) | UI data loads lack cancellation (nav race) | Reliability / UX | p2 | Naomi | Not started |
| [#12](https://github.com/DavidHoerster/dcr-explorer/issues/12) | Error UI shows bare ex.Message, no retry | UX | p2 | Naomi | Not started |
| [#13](https://github.com/DavidHoerster/dcr-explorer/issues/13) | Empty/malformed Azure data proceeds silently | Correctness | p2 | Amos | Not started |
| [#14](https://github.com/DavidHoerster/dcr-explorer/issues/14) | No service-layer input validation / null guards | Correctness | p2 | Holden | Not started |
| [#15](https://github.com/DavidHoerster/dcr-explorer/issues/15) | Centralize hard-coded ARM API versions | Maintainability | p2 | Holden | Not started |
| [#16](https://github.com/DavidHoerster/dcr-explorer/issues/16) | Pin Mermaid CDN to exact version | Security / Maintainability | p2 | Naomi | Not started |
| [#17](https://github.com/DavidHoerster/dcr-explorer/issues/17) | Accessibility gaps in report table & diagram | Accessibility | p2 | Naomi | Not started |
| [#18](https://github.com/DavidHoerster/dcr-explorer/issues/18) | Decompose ~800-line Report.razor | Refactor | p2 | Naomi | Not started |

### backlog — Low priority / deferred

| Issue | Title | Type | Priority | Owner | Status |
|-------|-------|------|----------|-------|--------|
| [#19](https://github.com/DavidHoerster/dcr-explorer/issues/19) | Remove Counter/Weather scaffold pages | Cleanup | low | Naomi | In review |
| [#20](https://github.com/DavidHoerster/dcr-explorer/issues/20) | Add ConfigureAwait(false) in service awaits | Maintainability | low | Amos | Not started |
| [#21](https://github.com/DavidHoerster/dcr-explorer/issues/21) | Extract magic strings to constants | Maintainability | low | Amos | Not started |
| [#22](https://github.com/DavidHoerster/dcr-explorer/issues/22) | MermaidDiagram.DisposeAsync swallows all exceptions | Bug | low | Naomi | Not started |
| [#23](https://github.com/DavidHoerster/dcr-explorer/issues/23) | Add report export (CSV/Excel) | Feature | low | Naomi | Not started |
| [#24](https://github.com/DavidHoerster/dcr-explorer/issues/24) | Spike: migrate to Azure.ResourceManager SDK | Spike | low | Holden | Not started |

---

## Status legend

| Status | Meaning |
|--------|---------|
| Not started | No work begun |
| In progress | Actively being worked |
| In review | PR open / awaiting review |
| Done | Merged and verified |

---

## How this plan is maintained

- **GitHub issues are the source of truth.** Each row links to its canonical
  issue in `DavidHoerster/dcr-explorer`.
- **This file mirrors the release grouping** so the team has one human-readable
  roadmap of what lands in each release.
- **Update the Status column** as work progresses (Not started → In progress →
  In review → Done). Keep statuses to the values in the legend so the table
  stays parseable.
- Scope is fixed to the code-review backlog (#2–#24). New work should be filed
  as a fresh GitHub issue and added here under the appropriate release.

---

_Generated from code review 2026-06-25; release triage 2026-06-26 by the Squad team (Lead: Holden)._
