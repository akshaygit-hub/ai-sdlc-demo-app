# Proposal

## Why

Invoice approval currently accepts a single approver without considering the approver's role or the invoice amount. Role-based limits and a second distinct approval for high-value invoices are needed to enforce the assignment's approval policy while preserving a clear audit trail and leaving invoice data untouched when an approval attempt is rejected.

## What Changes

- Apply the stated single-approver limits: a Manager may approve up to 50,000, and a Director may approve up to 500,000.
- Require two distinct approvers for amounts above 500,000; the second approver must be a Director, and one user must never count twice.
- Preserve approval identities and history, and leave the invoice unchanged when an attempt is rejected, with a clear reason.
- Pass the authenticated identity and role into Domain approval decisions; do not make Admin an implicit approval-limit bypass or map the legacy Approver role to Manager or Director.
- Add staged unit tests, a Playwright rejected over-limit scenario, review expectations, mutation testing, and AI interaction logging.

## Capabilities

### New Capabilities

- `invoice-approval-limits`: Role-based amount limits, high-value two-person approval, and unchanged invoice state on rejected attempts.

### Modified Capabilities

None. The repository has no existing OpenSpec capability specifications.

## Impact

- Domain: `ApprovalService` and `Invoice` approval state and history.
- API: the approvals controller must pass authenticated identity and role to Domain; existing sign-in and role authorization must not confer an Admin bypass.
- Tests: `ApprovalServiceTests`, `FakeInvoiceRepository`, and Playwright E2E coverage using existing page-object and accessible-locator conventions.
- The eligibility of the first approver for an invoice above 500,000 is not fully specified. The second approver must be a Director, but the assignment does not state whether the first approver may be a Manager despite the 50,000 single-approver limit. Human decision: keep this unresolved, document it, and gate implementation of this path on an explicit product decision. Do not silently add a first-approver Director requirement or another eligibility restriction.
