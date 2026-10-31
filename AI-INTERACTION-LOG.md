# AI Interaction Log

## Purpose and conventions

This log records the AI-assisted SDLC activities documented for this assignment. Historical details are recorded from the activity information available; missing information is explicitly marked `unavailable` or `null` rather than inferred. Historical validation results are identified as reported evidence and are not presented as rerun. No commit was created for these activities.

The JSONL companion contains one JSON object per interaction, with the same interaction IDs as this log. Timestamps are ISO 8601 when known; otherwise they are `null`. Lists are empty only when the activity record establishes that no item occurred; unavailable details are otherwise explicitly marked.

## AI tool, model, agent, and skills

| Interaction | AI tool | Model | Agent type / mode | Skills |
|---|---|---|---|---|
| A | GitHub Copilot in VS Code | unavailable | unavailable / unavailable | unavailable |
| B | GitHub Copilot in VS Code | unavailable | unavailable / unavailable | `agent-customization` |
| C | GitHub Copilot in VS Code | unavailable | unavailable / unavailable | unavailable |
| D | GitHub Copilot in VS Code | unavailable | unavailable / unavailable | `agent-customization` |

## Interactions

### A — Initial repository analysis

- **Timestamp:** `null` (unavailable).
- **Prompt:** Original exact prompt unavailable. Recorded activity: read-only repository analysis.
- **Files inspected:** Repository structure, implementation, tests, and E2E setup; exact historical file list unavailable.
- **Files modified:** None. No application files were modified.
- **Sub-agents / MCP tools:** unavailable.
- **AI output:** Identified the .NET framework mismatch and the single-approver limitation.
- **Human decision:** Target `net10.0` because .NET SDK 10.0.303 is installed; the seven baseline tests passed.
- **Validation:** Seven baseline tests passed, as reported in the activity record; not rerun for this log task.
- **Outcome / commit:** Read-only analysis completed; no commit was created.

### B — AGENTS.md update

- **Timestamp:** `null` (unavailable).
- **Prompt:** Exact prompt unavailable. Recorded activity: update `AGENTS.md` with approval rules and testing conventions.
- **Files inspected:** `AGENTS.md`; `backend/src/InvoiceApp.Domain/ApprovalService.cs`; `backend/src/InvoiceApp.Domain/Invoice.cs`; `backend/src/InvoiceApp.Api/Controllers/ApprovalsController.cs`; `backend/src/InvoiceApp.Api/Auth/RequireRoleAttribute.cs`; `backend/src/InvoiceApp.Api/Auth/UserSession.cs`; `backend/tests/InvoiceApp.Tests/ApprovalServiceTests.cs`; `backend/tests/InvoiceApp.Tests/FakeInvoiceRepository.cs`; and the API, Domain, and test project files.
- **Files modified:** `AGENTS.md`.
- **Skills:** `agent-customization`.
- **Sub-agents / MCP tools:** unavailable.
- **AI output:** Updated project guidance with approval rules and testing conventions.
- **Human decision:** The current assignment updates the role design to Manager and Director, with Admin retaining administration but no implicit approval bypass; the legacy Approver role is not assumed to be Director.
- **Validation:** `git diff --check` passed, as reported in the activity record. No tests were run because this was documentation-only.
- **Outcome / commit:** Guidance updated; no commit was created.

### C — Authorization investigation

- **Timestamp:** `null` (unavailable).
- **Prompt:** Exact prompt unavailable. Recorded activity: investigate authorization, session identity, approval behavior, and relevant UI.
- **Files inspected:** `backend/src/InvoiceApp.Api/Auth/UserSession.cs`; `backend/src/InvoiceApp.Api/Auth/RequireRoleAttribute.cs`; `backend/src/InvoiceApp.Api/Controllers/AuthController.cs`; `backend/src/InvoiceApp.Api/Auth/SessionStore.cs`; `backend/src/InvoiceApp.Api/appsettings.json`; `backend/src/InvoiceApp.Api/Controllers/ApprovalsController.cs`; `backend/src/InvoiceApp.Domain/ApprovalService.cs`; and relevant UI code in `backend/src/InvoiceApp.Api/wwwroot/app.js` and `backend/src/InvoiceApp.Api/wwwroot/index.html`.
- **Files modified:** None.
- **Skills:** unavailable.
- **Sub-agents / MCP tools:** unavailable.
- **AI output:** Confirmed roles are strings and are currently `Approver` and `Admin`; the session contains username and role, while `ApprovalService` currently receives only username; the invoice currently stores only one decision identity.
- **Human decision:** Use Manager and Director as approval roles; retain Admin for administration without an implicit approval bypass.
- **Validation:** Read-only investigation; no files modified.
- **Outcome / commit:** Authorization findings recorded; no commit was created.

### D — Assignment documentation and interaction logs

- **Timestamp:** 2026-10-31T22:19:56.394+05:30.
- **Prompt:** Create the requested Markdown and JSONL interaction logs, correct `AGENTS.md` for the net10.0 assignment environment and Manager/Director approval design, do not modify application code or commit, and run `git diff --check`.
- **Files inspected:** `AGENTS.md`; `backend/src/InvoiceApp.Domain/ApprovalService.cs`; `backend/src/InvoiceApp.Domain/Invoice.cs`; `backend/src/InvoiceApp.Domain/InvoiceApp.Domain.csproj`; `backend/src/InvoiceApp.Api/Controllers/ApprovalsController.cs`; `backend/src/InvoiceApp.Api/Controllers/AuthController.cs`; `backend/src/InvoiceApp.Api/Auth/RequireRoleAttribute.cs`; `backend/src/InvoiceApp.Api/Auth/UserSession.cs`; `backend/src/InvoiceApp.Api/Auth/SessionStore.cs`; `backend/src/InvoiceApp.Api/appsettings.json`; `backend/src/InvoiceApp.Api/InvoiceApp.Api.csproj`; `backend/src/InvoiceApp.Api/wwwroot/app.js`; `backend/src/InvoiceApp.Api/wwwroot/index.html`; `backend/tests/InvoiceApp.Tests/ApprovalServiceTests.cs`; `backend/tests/InvoiceApp.Tests/FakeInvoiceRepository.cs`; `backend/tests/InvoiceApp.Tests/InvoiceApp.Tests.csproj`; `e2e/package.json`; and `e2e/playwright.config.ts`.
- **Files modified:** `AGENTS.md`; `AI-INTERACTION-LOG.md`; `AI-INTERACTION-LOG.jsonl`.
- **Skills:** `agent-customization`.
- **Sub-agents / MCP tools:** None.
- **AI output:** Updated repository guidance and created both requested interaction logs. No application source, project files, tests, or dependencies were changed.
- **Human decision:** Follow the role design and constraints specified in the assignment; do not commit.
- **Validation:** `git diff --check` passed. The JSONL parsed as four objects with all required fields; no trailing whitespace was found. Tests were not run because changes are documentation and logs only.
- **Outcome / commit:** Requested documentation and logs created; no commit was created.

## Outcome and commit references

Only `AGENTS.md` and the two requested root-level logs are in scope for the documentation changes. No commit was created; there are no commit references for A–D.
