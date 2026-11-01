# Design

## Context

`ApprovalService.Approve` receives the authenticated username and role and applies the single-approval caps through 500,000, but currently approves each invoice in one decision with no approval-history collection. `ApprovalsController` gets both values from the authenticated session and restricts the approve action to Manager/Director; rejection retains its Approver/Admin authorization. `ApiControllerBase.Handle` maps `InvalidOperationException` to HTTP 409 and `ArgumentException` to HTTP 400 with an error message. Invoice data is held in memory; the web UI displays decision errors.

## Goals / Non-Goals

**Goals:**
- Keep role and amount policy in the Domain service, using authenticated identity and role.
- Preserve ordered approver identities for a high-value approval and keep rejected attempts atomic.
- Keep high-value invoices pending after the first decision and finalize them only after a distinct Director's second decision.
- Retain the existing approval endpoint shape where possible and return understandable errors.

**Non-Goals:**
- Change invoice submission, rejection semantics, authentication, persistence technology, or unrelated screens.
- Grant Admin an approval bypass or map the legacy Approver role to a new approval role.
- Change role authorization for unrelated endpoints or infer that the legacy Approver role is a Manager or Director.

## Decisions

1. **Pass session identity and role to Domain.** The API takes both values from the authenticated `UserSession`; it does not accept a caller-selected identity or role as approval authority. Domain owns amount checks and approval decisions. The approve action must not inherit an Admin bypass from its authorization attribute. The API can continue returning the invoice on success and a clear `{ error }` response for mapped 4xx failures. Keep the existing route unless implementation evidence requires otherwise.

2. **Represent approval history explicitly.** Add an ordered approval record to the invoice for each counted approval, containing the authenticated username, role, and decision time. Preserve both records for a completed high-value approval. Keep the existing `DecidedBy` and `DecidedAt` fields as the final-decision summary for compatibility; do not populate that summary or mark a high-value invoice approved after only one approval. There is no durable database migration because the repository is in-memory.

3. **Validate before changing invoice state.** Check the invoice's pending state, applicable single-approver limit or high-value stage, second approver role, and identity distinctness before changing status, final decision fields, or approval history. Rejections return the existing API's clear 4xx error format and must not call an update that changes invoice state. Ensure the check-and-record operation cannot count one identity twice if requests overlap.

4. **Separate single-approval caps from the high-value pathway.** For amounts up to 500,000, a Manager's single-approval cap remains 50,000 and a Director's remains 500,000. For amounts above 500,000, a Manager or Director may make the first approval regardless of those single-approval caps; the invoice remains pending. Only a distinct Director may make the second approval, which completes the invoice.

5. **Record the human resolution of the original ambiguity.** The original assignment wording did not settle the first approver's eligibility for amounts above 500,000. The human clarified that either a Manager or Director may act first in this two-approver pathway, and that the single-approval caps do not block recording that first decision. This explicit interpretation resolves the earlier implementation gate; it does not change the single-approval caps for invoices at or below 500,000.

## Risks / Trade-offs

- **Approval and rejection use different roles** → Keep approval limited to Manager/Director without an Admin bypass or legacy Approver mapping, and preserve the unrelated Approver/Admin rejection behavior.
- **Invoice and repository objects are mutable and in-memory** → Make validation and recording atomic for each invoice and test failed attempts against a complete before-and-after state snapshot.
- **Approval history adds response data** → Treat the history field as additive and preserve existing final-decision fields for callers that use them.
- **Process quality can be mistaken for behavior coverage** → Review both stages, run mutation testing against amount boundaries, duplicate identity, second-Director enforcement, and rejection immutability, and keep AI interaction logs current with unknown details explicitly unavailable.

## Migration Plan

No persisted data migration is required for the in-memory repository. Initialize approval history for new and existing in-memory invoices, retain the current decision fields, and verify serialization of the additive history field. Roll back by reverting the feature change; the application has no durable approval records to migrate.

## Open Questions

- **Resolved product interpretation:** The original assignment did not specify the first approver's eligibility above 500,000. The human clarified that either a Manager or Director can make the first decision in the two-approver pathway, notwithstanding the single-approval caps; the second decision must be by a distinct Director.
