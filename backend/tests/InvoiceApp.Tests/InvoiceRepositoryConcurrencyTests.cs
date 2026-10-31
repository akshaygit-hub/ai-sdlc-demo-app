using InvoiceApp.Api;
using InvoiceApp.Domain;
using Xunit;

namespace InvoiceApp.Tests;

public class InvoiceRepositoryConcurrencyTests
{
    private sealed class BlockingLookupRepository : IInvoiceRepository, IDisposable
    {
        private readonly InMemoryInvoiceRepository _inner = new();
        private readonly string _blockedInvoiceId;

        public BlockingLookupRepository(string blockedInvoiceId) => _blockedInvoiceId = blockedInvoiceId;

        public ManualResetEventSlim InvoiceLookupReturned { get; } = new();
        public ManualResetEventSlim ContinueLookup { get; } = new();

        public IReadOnlyList<Invoice> GetAll() => _inner.GetAll();

        public Invoice? GetById(string id)
        {
            var invoice = _inner.GetById(id);
            if (id == _blockedInvoiceId && invoice is not null)
            {
                InvoiceLookupReturned.Set();
                ContinueLookup.Wait();
            }

            return invoice;
        }

        public IReadOnlyList<Invoice> GetByIds(IEnumerable<string> ids) => _inner.GetByIds(ids);
        public Invoice Add(Invoice invoice) => _inner.Add(invoice);
        public void Update(Invoice invoice) => _inner.Update(invoice);
        public bool Remove(string id) => _inner.Remove(id);

        public void Dispose()
        {
            ContinueLookup.Set();
            InvoiceLookupReturned.Dispose();
            ContinueLookup.Dispose();
        }
    }

    [Theory]
    [InlineData(InvoiceStatus.Draft, false)]
    [InlineData(InvoiceStatus.PendingApproval, true)]
    public void Delete_BetweenServiceLookupAndMutation_DoesNotAllowMutationToSucceed(
        InvoiceStatus initialStatus,
        bool approve)
    {
        const string invoiceId = "INV-RACE";
        using var repository = new BlockingLookupRepository(invoiceId);
        repository.Add(new Invoice
        {
            Id = invoiceId,
            CustomerName = "Test School",
            Amount = 100m,
            Status = initialStatus
        });

        var service = new ApprovalService(repository);
        Exception? operationError = null;
        var operationSucceeded = false;
        var operationThread = new Thread(() =>
        {
            try
            {
                if (approve)
                {
                    service.Approve(invoiceId, "demo.manager", "Manager");
                }
                else
                {
                    service.Submit(invoiceId);
                }

                operationSucceeded = true;
            }
            catch (Exception exception)
            {
                operationError = exception;
            }
        });

        operationThread.Start();
        try
        {
            Assert.True(repository.InvoiceLookupReturned.Wait(TimeSpan.FromSeconds(5)));
            Assert.True(repository.Remove(invoiceId));
        }
        finally
        {
            repository.ContinueLookup.Set();
            Assert.True(operationThread.Join(TimeSpan.FromSeconds(5)));
        }

        Assert.False(operationSucceeded);
        Assert.IsType<KeyNotFoundException>(operationError);
        Assert.Null(repository.GetById(invoiceId));
    }
}
