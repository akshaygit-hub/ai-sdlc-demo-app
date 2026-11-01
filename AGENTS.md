# Project conventions for AI agents

Invoice approval demo app: the original project targeted ASP.NET Core 8, but this assignment environment targets `net10.0` because .NET SDK 10.0.303 is installed. It is a Web API with a small static web UI.

## Layout

- `backend/src/InvoiceApp.Domain`: business rules, no web or storage code. `ApprovalService` owns the invoice lifecycle (Draft, PendingApproval, Approved, Rejected).
- `backend/src/InvoiceApp.Api`: controllers, sign-in (`Auth/`), in-memory repository, and the web UI under `wwwroot/`.
- `backend/tests/InvoiceApp.Tests`: xUnit tests, one test class per class under test, named `<ClassUnderTest>Tests`.
- `e2e`: Playwright end-to-end tests (`tests/`) and page objects (`pages/`).
- `docs/use-cases`: short briefs for features we may build.

## Conventions

- Controllers live in `Controllers/`, one per resource, route `api/[controller]`. Request bodies are `record` types declared inside the controller.
- Controllers inherit `ApiControllerBase` and use `Handle(...)` so service errors map to 404, 409 and 400.
- Every endpoint is behind sign-in. Restrict actions with `[RequireRole("...")]`: destructive actions are Admin only; Manager and Director are the approval roles. Admin retains administrative permissions, but the Admin role alone does not grant approval or bypass approval limits.
- Business rules go in the Domain project, never in controllers.
- Register services in `Program.cs`. Repositories are interfaces in Domain with an in-memory implementation in Api.
- Every new rule or endpoint needs tests. Use `FakeInvoiceRepository` in tests.
- Use clear names; no one-letter variables or `flag` parameters.
- End-to-end tests use page objects and role-based locators (`getByRole`, `getByLabel`), never CSS or XPath selectors.

## Role-based approval rules

- Keep approval business rules in `backend/src/InvoiceApp.Domain`; do not put them in controllers or `Program.cs`.
- Pass the authenticated user's identity and role to the Domain approval service. Keep all monetary limits and approval decisions in Domain, not in controllers or authorization attributes.
- Managers may approve invoices up to 50,000. Directors may approve invoices up to 500,000. Invoices above 500,000 require two distinct approvals, and the second approval must be from a Director.
- The same user must not approve an invoice twice. Preserve both approvers' identities and the approval history. Do not assume the legacy `Approver` role is equivalent to `Director`.
- Validate every approval rule before mutating invoice status or approval history. A failed approval attempt must return a clear reason and leave both unchanged.
- Test boundary values, positive and negative cases, and authorization. Add Playwright E2E coverage for a rejected over-limit approval.
- Keep implementation changes small and independently testable. E2E tests must use page objects and accessible role/label locators (`getByRole`, `getByLabel`), never CSS or XPath selectors.

## Commands

- Run the app: `dotnet run --project backend/src/InvoiceApp.Api` then open http://localhost:5000
- Run the tests: `dotnet test backend/InvoiceApp.sln`
- Run end-to-end tests: `cd e2e && npm install && npx playwright install chromium && npm test`

## Demo accounts (not real credentials)

- `demo.approver` / `ChangeMe123!` (role Approver)
- `demo.admin` / `ChangeMe123!` (role Admin)
- The current `demo.approver` role is legacy configuration; do not treat it as Manager or Director without an explicit role change.

## Never

- Commit real credentials, tokens or customer data.
- Add business logic to `Program.cs` or controllers.
