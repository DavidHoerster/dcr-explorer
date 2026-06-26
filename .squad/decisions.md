# Squad Decisions

## Active Decisions

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
