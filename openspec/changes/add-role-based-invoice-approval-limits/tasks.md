# Tasks

## 1. Stage 1 — Single-approver limits

- [ ] 1.1 Pass the authenticated username and role from the approval API into Domain and implement Manager (<= 50,000) and Director (<= 500,000) single-approval decisions there; verify role and monetary decisions are not implemented in the controller.
- [ ] 1.2 Add `ApprovalServiceTests` for Manager at 50,000 and above 50,000, Director at 500,000 and above 500,000, and attempts by Admin and legacy Approver; verify positive outcomes and clear rejection reasons with unchanged invoice state using `FakeInvoiceRepository`.
- [ ] 1.3 Run `dotnet test backend/InvoiceApp.sln` for the stage; verify the stage's tests pass and update `AI-INTERACTION-LOG.md` and `AI-INTERACTION-LOG.jsonl` with the actual prompt, available model/tool details, files, human decisions, and validation, marking unavailable details explicitly.
- [ ] 1.4 Create and use multiple agents and sub-agents with distinct responsibilities: one for implementation, one for test review, and one for independent code review; verify each produces a recorded output, Stage 1 findings are recorded, and actionable findings are resolved or explicitly documented before Stage 2.
- [ ] 1.5 Run the available approved mutation-testing tool against the single-approval amount boundaries and role checks; verify the relevant mutants are killed, or request approval to add/configure a mutation tool if none is available, and log the result.
- [ ] 1.6 Review every PR finding and record exactly one verdict per finding: fixed, dismissed with a reason, or deferred with a reason; verify no finding is left without exactly one verdict.

## 2. Stage 2 — Two-approver rule

- [ ] 2.1 Obtain an explicit product decision on which role may make the first approval above 500,000; verify the decision is recorded in the spec and design before implementing or testing first-approver role eligibility, without defaulting to a Director-only first approval.
- [ ] 2.2 Add ordered approval history to invoice state and implement the high-value pending-to-approved flow with two distinct identities and a Director as the second approver; verify one approval leaves the invoice pending and both identities remain available after completion.
- [ ] 2.3 Add unit tests for a distinct Director second approval, a non-Director second approver, and the same user attempting both approvals; snapshot all invoice fields and approval history to verify every rejected attempt returns a clear reason and causes no mutation.
- [ ] 2.4 Run `dotnet test backend/InvoiceApp.sln` for Stage 2; verify the full suite passes and update both AI interaction logs with actual stage prompts, available model/tool details, changed files, decisions, and validation.
- [ ] 2.5 Run the available approved mutation-testing tool against the second-Director check, identity distinctness, approval count, and rejection immutability; verify targeted mutants are killed, or request approval to add/configure a mutation tool if none is available, and log the result.
- [ ] 2.6 Have the test-review agent and independent code-review sub-agent review Stage 2 changes; verify both review outputs cover the product decision, two-user history, and unchanged-on-rejection behavior, and actionable findings are resolved or explicitly documented.
- [ ] 2.7 Review every PR finding and record exactly one verdict per finding: fixed, dismissed with a reason, or deferred with a reason; verify no finding is left without exactly one verdict.

## 3. API and end-to-end verification

- [ ] 3.1 Apply approval-role authorization using the authenticated session without an Admin bypass or legacy Approver mapping, while keeping amount decisions in Domain; verify API tests cover the authenticated role and the API returns a clear 4xx error without invoice mutation on rejected attempts.
- [ ] 3.2 Add a Playwright E2E test using page objects and accessible role/label locators in which a Manager attempts an over-limit approval; verify the UI displays the rejection reason and the invoice remains pending and unchanged.
- [ ] 3.3 Run `dotnet test backend/InvoiceApp.sln` and the configured Playwright E2E suite; verify both pass, record the results and file changes in the AI interaction logs, and run `git diff --check`.
- [ ] 3.4 Request a final agent review of the integrated API, Domain, and E2E changes; verify findings and dispositions are included in the review record and AI interaction logs.
- [ ] 3.5 Verify all final assignment deliverables, including `ASSIGNMENT.md` and the 8–10-minute demo video link; confirm each is present and complete or record the specific missing deliverable for resolution.
