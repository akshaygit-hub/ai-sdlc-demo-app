# Independent Code Review

**Review scope:** Complete `impl` branch changes compared with `main` (`main...HEAD`).  
**Review type:** Read-only senior .NET code review. No implementation files were changed.

## Findings

### 1. Shared demo credentials can weaken two-approver separation of duties

- **Severity:** Medium
- **File / lines:** [`appsettings.json`](backend/src/InvoiceApp.Api/appsettings.json#L12-L14)
- **Problem and impact:** The configured Manager and Director demo accounts use the same password. Since authentication creates sessions from the configured username/password and the high-value approval rule distinguishes approvers by username, a person who knows the demo password can sign in as two different configured users and complete both approvals. Thus the distinct-identity check does not, by itself, establish that two different people approved.
- **Suggested correction:** Keep demo credentials development-only; for shared or deployed environments, use separately provisioned credentials from deployment secrets and ensure the accounts are controlled by distinct authorized users. Do not treat this as a change to the Domain identity rule.
- **Context:** These are documented demo accounts, not real credentials. This is a deployment/security observation, not evidence that the approval logic accepts the same username twice.

## Business requirements

| Requirement | Verdict | Review evidence |
|---|---|---|
| Manager single approval through 50,000 inclusive | Satisfied | Domain limit check and exact-boundary unit tests |
| Director single approval through 500,000 inclusive | Satisfied | Domain limit check and exact-boundary unit tests |
| Amounts above 500,000 require two distinct approvers | Satisfied | High-value approval history and identity check |
| Second approver must be a Director | Satisfied | Domain validates role before recording the second decision |
| Same user cannot approve twice | Satisfied | Case-insensitive identity comparison and duplicate-user test |
| Rejected attempt gives a clear reason and does not mutate invoice/history | Satisfied | Validation precedes mutation; tests snapshot the invoice and assert no repository update for rejected approvals |

The approved interpretation permits a Manager or Director to make the first high-value approval; the second approval remains restricted to a distinct Director. This review found no implementation change that contradicts that interpretation.

## Test coverage observations

The backend suite covers amount boundaries, allowed and rejected roles, successful high-value approval, duplicate identities, non-Director second approvals, rejected-state immutability, snapshots, and concurrency/deletion cases. The Playwright test exercises the Manager over-limit rejection, checks the displayed reason, compares authenticated invoice JSON before and after, and verifies Pending Approval remains visible.

Additional valuable coverage:

- API-level tests for unauthenticated access, Admin and legacy Approver denial on approval, and authenticated username/role propagation to Domain.
- A successful high-value Playwright flow using two separately authenticated users.

These are coverage opportunities; the reviewed evidence does not establish a defect in the current authorization or Domain behavior.

## Validation evidence

The review run reported `dotnet test backend/InvoiceApp.sln`: **32 passed, 0 failed**; Playwright `npm test`: **1 passed**; and `git diff --check`: passed. The repository history records that a later full test invocation against the shared build output was blocked by the running API process locking a DLL; the process was left running and the suite passed from an isolated temporary copy.

## Overall verdict

**PASS WITH OBSERVATIONS.** The listed approval rules are implemented and covered by unit tests, with a rejected over-limit scenario covered in Playwright. The shared demo credential configuration should be restricted to development or replaced with separately controlled credentials outside a local demo environment. API authorization tests and a successful high-value E2E flow would improve confidence.

This report is a branch code review; it does not claim that a pull request exists or has been reviewed.
