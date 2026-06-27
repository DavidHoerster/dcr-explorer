# Naomi — History

## Session: 2026-06-15 — Team formation

- Joined as Frontend Dev for dcr-detail-blazor
- Project: Blazor Server app with Mermaid diagram visualizations of Azure DCR mappings
- Key pages: Home.razor, Report.razor, WorkspacePicker.razor
- Shared components: MermaidDiagram.razor, SummaryCard.razor, LoadingSpinner.razor
- Layout components under Components/Layout/
- Stack: Blazor Server, C#/.NET 8, Mermaid.js
- User: David Hoerster

## Session: 2026-06-26 — Mermaid XSS hardening (#6)

- Pattern: Mermaid hardened with `securityLevel: 'strict'` + `htmlLabels: false` in `wwwroot/js/mermaid-interop.js`; `host.innerHTML = svg` only ever receives Mermaid's own strict-sanitized render output (no raw user text).
- Single-chokepoint pattern: all label escaping flows through `DcrDetailBlazor.Models.MermaidLabelHelper.EscapeLabel` (public, unit-testable). Report.razor's 8 call sites delegate to it — never inline-escape labels again, extend the helper instead.
- Helper hardening: collapse whitespace/control chars to single space, downgrade `"`/backtick to `'` (kills label-wrapper breakout), strip `< > [ ] { }`, cap 120 chars, return `—` for empty/stripped. 13 tests in `MermaidLabelHelperTests.cs`. Build clean, 37/37 tests. PR #27.
