# Spec Delta

## Purpose

This capability applies the assignment's role-based invoice approval limits, records distinct approvals for high-value invoices, and ensures rejected attempts do not change invoice state or approval history.

## ADDED Requirements

### Requirement: Single-approver limits are role-based
The system SHALL allow a Manager to make a single final approval only for invoices of 50,000 or less, and a Director to make a single final approval only for invoices of 500,000 or less. The authenticated identity and role determine the decision; Admin grants no implicit bypass, and legacy Approver is not treated as Manager or Director.

#### Scenario: Manager approves at the inclusive limit
- **WHEN** an authenticated Manager approves a pending invoice for 50,000
- **THEN** the invoice is approved and the Manager's identity is recorded

#### Scenario: Manager exceeds the single-approval limit
- **WHEN** an authenticated Manager attempts a single approval for an invoice of 50,000.01
- **THEN** the attempt is rejected with a clear reason and the invoice remains unchanged

#### Scenario: Director approves at the inclusive limit
- **WHEN** an authenticated Director approves a pending invoice for 500,000
- **THEN** the invoice is approved and the Director's identity is recorded

#### Scenario: Admin does not bypass approval limits
- **WHEN** an authenticated Admin attempts an approval using the Admin role
- **THEN** the Admin role alone does not grant approval or bypass a role limit

#### Scenario: Legacy Approver role is not mapped
- **WHEN** an authenticated user has the legacy Approver role and attempts an approval
- **THEN** the system does not treat that role as Manager or Director

### Requirement: High-value invoices need two distinct approvals
For an invoice above 500,000, the system SHALL require two approvals by different user identities, and the second approval SHALL be made by a Director. The system SHALL NOT require the first approver to be a Director.

#### Scenario: A distinct Director completes a high-value approval
- **WHEN** an invoice for 500,000.01 has one recorded approval and a different authenticated Director makes the second approval
- **THEN** both identities are preserved in order and the invoice becomes approved

#### Scenario: One approval is insufficient for a high-value invoice
- **WHEN** a high-value invoice has fewer than two distinct approvals
- **THEN** it remains pending approval and is not reported as approved

#### Scenario: Same user attempts both approvals
- **WHEN** the identity recorded for the first approval attempts to make the second approval
- **THEN** the attempt is rejected with a clear reason and the user is not counted twice

#### Scenario: Second approver is not a Director
- **WHEN** a user without the Director role attempts the second approval for a high-value invoice
- **THEN** the attempt is rejected with a clear reason

### Requirement: Rejected approval attempts do not mutate invoices
When an approval attempt is rejected, the system SHALL return a clear reason and SHALL leave the invoice status, decision data, approval history, and other invoice state unchanged.

#### Scenario: Rejected attempt preserves complete invoice state
- **WHEN** a role limit, duplicate identity, or second-approver role rule rejects an approval attempt
- **THEN** the response explains the rejection and a before-and-after comparison shows no invoice or approval-history changes

### Requirement: Approval decisions use authenticated session identity
The system SHALL make approval decisions using the authenticated user's identity and role, and SHALL return the resulting invoice state or a clear error through the approval API.

#### Scenario: Approval API records the authenticated identity
- **WHEN** an authenticated approval request succeeds
- **THEN** the returned invoice and its approval history identify the user from the authenticated session

#### Scenario: Rejected over-limit approval is visible in the UI
- **WHEN** a Manager attempts to approve an invoice above the Manager's single-approval limit in the web UI
- **THEN** the UI displays the clear rejection reason and the invoice remains pending and unchanged
