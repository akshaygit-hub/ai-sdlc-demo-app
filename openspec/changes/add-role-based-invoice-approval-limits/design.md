# Design

## Context

`ApprovalService.Approve` currently accepts only an approver name, immediately marks a pending invoice approved, and records one decision identity and timestamp. `Invoice` has no approval-history collection. The session already contains username and role, but `ApprovalsController` passes only the username and is class-authorized for the legacy `Approver` and `Admin` roles. `ApiControllerBase.Handle` maps `InvalidOperationException` to HTTP 409 and `ArgumentException` to HTTP 400 with an error message. Invoice data is held in memory; the web UI displays decision errors.

## Goals / Non-Goals

**Goals:**
- Keep role and amount policy in the Domain service, using authenticated identity and role.
- Preserve ordered approver identities for a high-value approval and keep rejected attempts atomic.
- Retain the existing approval endpoint shape where possible and return understandable errors.

**Non-Goals:**
- Change invoice submission, rejection semantics, authentication, persistence technology, or unrelated screens.
- Grant Admin an approval bypass or map the legacy Approver role to a new approval role.
- Implement high-value first-approver eligibility until the explicit product decision described below is made.

## Decisions

1. **Pass session identity and role to Domain.** The API takes both values from the authenticated `UserSession`; it does not accept a caller-selected identity or role as approval authority. Domain owns amount checks and approval decisions. The approve action must not inherit an Admin bypass from its authorization attribute. The API can continue returning the invoice on success and a clear `{ error }` response for mapped 4xx failures. Keep the existing route unless implementation evidence requires otherwise.

2. **Represent approval history explicitly.** Add an ordered approval record to the invoice for each counted approval, containing the authenticated username, role, and decision time. Preserve both records for a completed high-value approval. Keep the existing `DecidedBy` and `DecidedAt` fields as the final-decision summary for compatibility; do not populate that summary or mark a high-value invoice approved after only one approval. There is no durable database migration because the repository is in-memory.

3. **Validate before changing invoice state.** Check the invoice's pending state, applicable single-approver limit or high-value stage, second approver role, and identity distinctness before changing status, final decision fields, or approval history. Rejections return the existing API's clear 4xx error format and must not call an update that changes invoice state. Ensure the check-and-record operation cannot count one identity twice if requests overlap.

4. **Implement in independently testable stages.** Stage 1 implements the single-approver limits through 500,000 and their Domain tests. An amount above 500,000 must not be finalized by a single approval. Stage 2 implements the two-approver state and tests only after the eligibility decision gate is satisfied. Keep the initial high-value eligibility policy isolated so resolving it cannot move monetary limits out of Domain.

5. **Leave the first high-value approver's eligibility unresolved.** The human chose to preserve this ambiguity rather than infer an interpretation. The requirement does not require the first approver to be a Director, and it does not establish whether a Manager may make the first approval above 50,000. Do not implement or test a definitive first-role eligibility rule until a product decision is recorded. This gate must not be converted into a first-Director requirement by default.

## Risks / Trade-offs

- **High-value approvals remain incomplete until the eligibility gate is resolved** → Do not claim Stage 2 is complete or enable a guessed role rule; record the decision in the AI interaction log before implementation proceeds.
- **Existing API authorization is class-level and uses legacy roles** → Scope role authorization for the approval action without introducing an Admin amount bypass or silently mapping Approver to Director; preserve unrelated rejection behavior.
- **Invoice and repository objects are mutable and in-memory** → Make validation and recording atomic for each invoice and test failed attempts against a complete before-and-after state snapshot.
- **Approval history adds response data** → Treat the history field as additive and preserve existing final-decision fields for callers that use them.
- **Process quality can be mistaken for behavior coverage** → Review both stages, run mutation testing against amount boundaries, duplicate identity, second-Director enforcement, and rejection immutability, and keep AI interaction logs current with unknown details explicitly unavailable.

## Migration Plan

No persisted data migration is required for the in-memory repository. Initialize approval history for new and existing in-memory invoices, retain the current decision fields, and verify serialization of the additive history field. Roll back by reverting the feature change; the application has no durable approval records to migrate.

## Open Questions

- **Blocking product decision:** Which role(s) may make the first approval for an invoice above 500,000? The requirement only states that the second approver must be a Director. The human explicitly chose to leave this unresolved and gate implementation rather than assume that the first approver must be a Director or that a Manager's 50,000 single-approval limit is overridden.
