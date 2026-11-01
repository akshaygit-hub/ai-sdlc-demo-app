# Invoice Approval Limits — Assignment Summary

## 1. Assignment overview

This assignment extends the invoice approval demo with role-based approval limits and a two-person approval path for high-value invoices. The implemented rules are:

- A Manager may give a single final approval up to and including **50,000**.
- A Director may give a single final approval up to and including **500,000**.
- An invoice above **500,000** requires two distinct approvers; only a Director may provide the second approval.
- Admin does not bypass approval rules, and the legacy Approver role is not mapped to Manager or Director.
- Rejected approval attempts return a clear reason and leave invoice state and approval history unchanged.

## 2. Architecture and implementation

The implementation keeps approval decisions in the Domain layer:

- `backend/src/InvoiceApp.Domain/ApprovalService.cs` validates role, amount, invoice status, and high-value approval history. A first high-value approval leaves the invoice pending; the distinct Director's second approval completes it.
- `backend/src/InvoiceApp.Domain/Invoice.cs` holds ordered approval records (approver, role, UTC decision time), final decision fields, and creates a snapshot under the per-invoice lock.
- `backend/src/InvoiceApp.Api/Controllers/ApprovalsController.cs` passes the authenticated session username and role to Domain. Approval is restricted to Manager/Director; rejection retains its existing Approver/Admin role restriction.
- `backend/src/InvoiceApp.Api/Controllers/InvoicesController.cs` exposes invoice list/detail/create/submit/delete routes under `api/invoices`. Approval and rejection are exposed under `api/approvals/{invoiceId}/approve` and `/reject`. API reads return snapshots; deletion is Admin-only.
- The in-memory repository and per-invoice synchronization protect service mutations and snapshots from concurrent reads/deletion.

## 3. Requirements interpretation

The original assignment did not settle who may make the first approval above 500,000. The human-approved interpretation, recorded in the OpenSpec design and specification, allows either a Manager or a Director to make that first approval in the two-approver flow, even above the single-approval cap. The second approval must come from a **distinct Director**. The first approval leaves the invoice Pending Approval. This interpretation does not change the single-approval limits for invoices at or below 500,000.

## 4. Specification-driven development

The OpenSpec change [`add-role-based-invoice-approval-limits`](openspec/changes/add-role-based-invoice-approval-limits/) contains a proposal, design, capability specification, and staged task breakdown. It records boundaries, positive and negative scenarios, rejected-attempt immutability, E2E and mutation-testing expectations, review activities, and final deliverables. The remaining checklist items and submission deliverables should be verified before submission; this summary does not claim that every OpenSpec task is complete.

## 5. AI-assisted development workflow

[`AGENTS.md`](AGENTS.md) records repository conventions, role rules, architecture boundaries, and test/E2E practices. The activity audit is in [`AI-INTERACTION-LOG.md`](AI-INTERACTION-LOG.md) and [`AI-INTERACTION-LOG.jsonl`](AI-INTERACTION-LOG.jsonl); unavailable historical model and mode details are marked unavailable.

Recorded workflow included `agent-customization` and `run-tests` skills, a general-purpose implementation sub-agent, and an independent code-review sub-agent. Independent review findings were investigated and addressed in later work. **Hook-based workflow requirement: Incomplete.** No hook implementation or evidence of hook execution was found; the repository contains only Git's sample hook files. A separate test-review agent is not claimed here.

## 6. Testing

The backend xUnit suite covers inclusive boundaries, over-limit rejection, disallowed roles, first and second high-value approvals, approval order and history, duplicate identities, non-Director second approvals, rejected-state immutability, snapshots, and concurrency/deletion cases.

- **Backend:** 32 passed, 0 failed, 0 skipped. The requested in-place full test build was blocked because the already-running API process locked a DLL. The API process was not terminated; the full suite passed when run from an isolated temporary copy of the backend.
- **Playwright:** 1 E2E test passed. As Manager, it creates and submits a unique 50,000.01 invoice, attempts approval in the UI, checks the exact rejection reason, compares complete authenticated invoice JSON before and after, and verifies the invoice remains Pending Approval.

## 7. Mutation testing

Stryker.NET **4.13.0** was used with `ApprovalService.cs` as the mutation scope.

| Run | Score | Evidence |
|---|---:|---|
| Baseline | 60.00% | 20 mutants tested; 7 tests discovered |
| Initial final | 86.89% | 58 mutants tested; 53 killed; 5 survived |
| After additional tests | 93.44% | 60 mutants tested; 56 killed; 3 survived; 1 timed out |
| Latest final | 96.72% | 60 mutants tested; 59 killed; 1 survived; 1 no coverage; 4 compile errors; 0 timeouts |

The remaining survivor is Mutation 30, which changes a rejection-message branch in the single-approval path. Invoices above 500,000 enter the approved two-approver flow before that branch; making the branch reject such a first approval would conflict with the approved interpretation. It is therefore intentionally left as a survivor, not used to justify changing business behavior.

The baseline discovered 7 tests, while the final suite had 32. The suite size and coverage differ, so the baseline-to-final score comparison is informative but not perfectly controlled. The latest report is [`mutation-report.json`](artifacts/mutation-testing/final-after-repository-assertions-4.13.0/reports/mutation-report.json).

## 8. Environment deviation

The original repository targeted .NET 8. The assignment environment could not install .NET 8 and provided .NET SDK **10.0.303**; the project targets were therefore changed to `net10.0` in commit `7674687`. During final validation, the running API process locked a DLL in the shared build output. It was left running. Tests were executed successfully in an isolated temporary copy instead; this limitation is recorded in the interaction logs.

## 9. Git commits

Relevant commits from repository history:

- `7674687` — `chore: target .NET 10 SDK for assignment`
- `cf01fb6` — `docs: add AI workflow instructions and interaction logs`
- `a554584` — `docs: add OpenSpec approval limits specification`
- `38a4c35` — `feat: enforce role-based invoice approval limits`
- `9cbf760` — `feat: require two distinct approvers for high-value invoices`
- `9488620` — `test: strengthen approval mutation coverage`
- `c9ff855` — `test: add Playwright invoice approval rejection E2E`
- `0e2fa7d` — `docs: update AI interaction audit logs`

## 10. AI interaction audit

The interaction records, prompts/details available, tools, skills, human decisions, validation, and commit references are maintained in [`AI-INTERACTION-LOG.md`](AI-INTERACTION-LOG.md) and [`AI-INTERACTION-LOG.jsonl`](AI-INTERACTION-LOG.jsonl). Unknown historical details are explicitly marked unavailable or null rather than reconstructed.

## 11. Independent review findings and dispositions

**Branch review verdict: PASS WITH OBSERVATIONS.** An independent senior .NET review compared the `impl` branch with `main`. The workshop's specific review prompt was unavailable, so a custom review prompt was used. This was a branch review; no PR URL or PR review is claimed.

| Finding / recommendation | Disposition | Reason |
|---|---|---|
| Medium-severity observation: shared demo credentials may let one person authenticate as two configured identities, weakening separation of duties. | **Deferred** | The finding remains open. The accounts are demo configuration, and no credential-management or account-ownership changes were authorized or implemented in this documentation update. The Domain distinct-username check does not prove distinct human operators. |
| Add API authorization tests for unauthenticated access, Admin/legacy Approver denials, and authenticated identity/role propagation. | **Deferred** | The review identified this as a coverage recommendation. No API authorization tests were added in the read-only review or this documentation-only follow-up. |
| Add a successful high-value Playwright scenario using two separately authenticated users. | **Deferred** | The review identified this as a coverage recommendation. The existing E2E covers rejected Manager over-limit approval; a successful two-user browser flow was not implemented. |

No finding is recorded as fixed or dismissed.

## 12. Demo video

[AkshayKumarKushawaha-3216168-AI in SDLC.mp4](https://nagarro-my.sharepoint.com/:v:/r/personal/akshay_kushawaha_nagarro_com/Documents/AI%20in%20SDLC%20Assignment/AkshayKumarKushawaha-3216168-AI%20in%20SDLC.mp4?d=w8d3463c3c5e247f280b4274ff4ef4615&csf=1&web=1&nav=eyJyZWZlcnJhbEluZm8iOnsicmVmZXJyYWxBcHAiOiJPbmVEcml2ZUZvckJ1c2luZXNzIiwicmVmZXJyYWxBcHBQbGF0Zm9ybSI6IldlYiIsInJlZmVycmFsTW9kZSI6InZpZXciLCJyZWZlcnJhbFZpZXciOiJNeUZpbGVzTGlua0NvcHkifX0&e=Np8xPt)
