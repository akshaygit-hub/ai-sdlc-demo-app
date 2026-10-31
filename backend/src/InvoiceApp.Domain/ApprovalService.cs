namespace InvoiceApp.Domain;

/// <summary>
/// Moves an invoice through its lifecycle: Draft -> PendingApproval -> Approved or Rejected.
/// </summary>
public class ApprovalService
{
    private readonly IInvoiceRepository _repository;

    public ApprovalService(IInvoiceRepository repository)
    {
        _repository = repository;
    }

    public Invoice Submit(string invoiceId)
    {
        var invoice = Find(invoiceId);

        lock (invoice)
        {
            if (invoice.Status != InvoiceStatus.Draft)
            {
                throw new InvalidOperationException(
                    $"Invoice {invoiceId} is {invoice.Status}; only drafts can be submitted.");
            }

            invoice.Status = InvoiceStatus.PendingApproval;
            _repository.Update(invoice);
            return invoice;
        }
    }

    public Invoice Approve(string invoiceId, string approver, string role)
    {
        var invoice = Find(invoiceId);

        lock (invoice)
        {
            EnsurePending(invoice);

            if (role is not ("Manager" or "Director"))
            {
                throw new InvalidOperationException(
                    $"Role '{role}' is not permitted to approve invoices.");
            }

            if (invoice.Amount > 500_000m)
            {
                return ApproveHighValue(invoice, approver, role);
            }

            var maximumAmount = role == "Manager" ? 50_000m : 500_000m;
            if (invoice.Amount > maximumAmount)
            {
                var rejectionReason = role == "Manager"
                    ? "Managers may approve invoices up to 50,000."
                    : "Invoices above 500,000 require the two-approver approval flow.";
                throw new InvalidOperationException(rejectionReason);
            }

            var decision = new ApprovalDecision(approver, role, DateTime.UtcNow);
            invoice.ApprovalHistory.Add(decision);
            CompleteApproval(invoice, decision);
            _repository.Update(invoice);
            return invoice;
        }
    }

    public Invoice Reject(string invoiceId, string approver, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("A reason is required to reject an invoice.", nameof(reason));
        }

        var invoice = Find(invoiceId);

        lock (invoice)
        {
            EnsurePending(invoice);

            invoice.Status = InvoiceStatus.Rejected;
            invoice.DecidedBy = approver;
            invoice.DecidedAt = DateTime.UtcNow;
            invoice.DecisionNote = reason;
            _repository.Update(invoice);
            return invoice;
        }
    }

    private Invoice Find(string invoiceId) =>
        _repository.GetById(invoiceId)
        ?? throw new KeyNotFoundException($"Invoice {invoiceId} was not found.");

    private Invoice ApproveHighValue(Invoice invoice, string approver, string role)
    {
        if (invoice.ApprovalHistory.Count == 0)
        {
            var firstDecision = new ApprovalDecision(approver, role, DateTime.UtcNow);
            invoice.ApprovalHistory.Add(firstDecision);
            _repository.Update(invoice);
            return invoice;
        }

        if (invoice.ApprovalHistory.Count != 1)
        {
            throw new InvalidOperationException(
                "A high-value invoice must have exactly one prior approval before its final approval.");
        }

        if (role != "Director")
        {
            throw new InvalidOperationException(
                "The second approval for an invoice above 500,000 must be made by a Director.");
        }

        if (string.Equals(invoice.ApprovalHistory[0].Approver, approver, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "A different user must provide the second approval for an invoice above 500,000.");
        }

        var decision = new ApprovalDecision(approver, role, DateTime.UtcNow);
        invoice.ApprovalHistory.Add(decision);
        CompleteApproval(invoice, decision);
        _repository.Update(invoice);
        return invoice;
    }

    private static void CompleteApproval(Invoice invoice, ApprovalDecision finalDecision)
    {
        invoice.Status = InvoiceStatus.Approved;
        invoice.DecidedBy = finalDecision.Approver;
        invoice.DecidedAt = finalDecision.DecidedAt;
    }

    private static void EnsurePending(Invoice invoice)
    {
        if (invoice.Status != InvoiceStatus.PendingApproval)
        {
            throw new InvalidOperationException(
                $"Invoice {invoice.Id} is {invoice.Status}; only pending invoices can be decided.");
        }
    }
}
