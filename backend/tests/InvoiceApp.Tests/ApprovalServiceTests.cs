using InvoiceApp.Domain;
using Xunit;

namespace InvoiceApp.Tests;

public class ApprovalServiceTests
{
    private static Invoice InvoiceWith(string id, InvoiceStatus status) =>
        new() { Id = id, CustomerName = "Test School", Amount = 100m, Status = status };

    private static (ApprovalService Service, FakeInvoiceRepository Repository) ServiceFor(params Invoice[] invoices)
    {
        var repository = new FakeInvoiceRepository(invoices);
        return (new ApprovalService(repository), repository);
    }

    private sealed record InvoiceSnapshot(
        string Id,
        string CustomerName,
        decimal Amount,
        InvoiceStatus Status,
        DateTime CreatedAt,
        string? DecidedBy,
        DateTime? DecidedAt,
        string? DecisionNote,
        ApprovalDecision[] ApprovalHistory);

    private static InvoiceSnapshot Snapshot(Invoice invoice) =>
        new(invoice.Id, invoice.CustomerName, invoice.Amount, invoice.Status, invoice.CreatedAt,
            invoice.DecidedBy, invoice.DecidedAt, invoice.DecisionNote, invoice.ApprovalHistory.ToArray());

    private static void AssertUnchanged(InvoiceSnapshot expected, Invoice invoice)
    {
        var actual = Snapshot(invoice);
        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.CustomerName, actual.CustomerName);
        Assert.Equal(expected.Amount, actual.Amount);
        Assert.Equal(expected.Status, actual.Status);
        Assert.Equal(expected.CreatedAt, actual.CreatedAt);
        Assert.Equal(expected.DecidedBy, actual.DecidedBy);
        Assert.Equal(expected.DecidedAt, actual.DecidedAt);
        Assert.Equal(expected.DecisionNote, actual.DecisionNote);
        Assert.Equal(expected.ApprovalHistory, actual.ApprovalHistory);
    }

    private sealed class BlockingInvoiceRepository : IInvoiceRepository, IDisposable
    {
        private readonly Invoice _invoice;
        private readonly ManualResetEventSlim _continueFinalApproval = new();
        private int _updateCount;

        public BlockingInvoiceRepository(Invoice invoice) => _invoice = invoice;

        public ManualResetEventSlim FinalApprovalUpdateStarted { get; } = new();
        public int UpdateCount => Volatile.Read(ref _updateCount);

        public IReadOnlyList<Invoice> GetAll() => new[] { _invoice };
        public Invoice? GetById(string id) => id == _invoice.Id ? _invoice : null;
        public IReadOnlyList<Invoice> GetByIds(IEnumerable<string> ids) =>
            ids.Contains(_invoice.Id) ? new[] { _invoice } : Array.Empty<Invoice>();
        public Invoice Add(Invoice invoice) => throw new NotSupportedException();
        public bool Remove(string id) => throw new NotSupportedException();

        public void Update(Invoice invoice)
        {
            if (Interlocked.Increment(ref _updateCount) == 2)
            {
                FinalApprovalUpdateStarted.Set();
                _continueFinalApproval.Wait();
            }
        }

        public void ContinueFinalApproval() => _continueFinalApproval.Set();

        public void Dispose()
        {
            ContinueFinalApproval();
            FinalApprovalUpdateStarted.Dispose();
            _continueFinalApproval.Dispose();
        }
    }

    [Fact]
    public void Submit_MovesDraftToPendingApproval()
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.Draft);

        var (service, _) = ServiceFor(invoice);
        var result = service.Submit("INV-1");

        Assert.Equal(InvoiceStatus.PendingApproval, result.Status);
    }

    [Fact]
    public void Submit_RejectsInvoiceThatIsNotADraft()
    {
        var (service, _) = ServiceFor(InvoiceWith("INV-1", InvoiceStatus.Approved));

        Assert.Throws<InvalidOperationException>(() => service.Submit("INV-1"));
    }

    [Fact]
    public void CreateSnapshot_WaitsForMutationAndCopiesCompleteInvoiceState()
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.PendingApproval);
        var decision = new ApprovalDecision("demo.director", "Director", DateTime.UtcNow);
        using var snapshotStarted = new ManualResetEventSlim();
        Invoice? snapshot = null;
        Thread? snapshotThread = null;

        Monitor.Enter(invoice);
        try
        {
            invoice.Status = InvoiceStatus.Approved;
            snapshotThread = new Thread(() =>
            {
                snapshotStarted.Set();
                snapshot = invoice.CreateSnapshot();
            });
            snapshotThread.Start();

            Assert.True(snapshotStarted.Wait(TimeSpan.FromSeconds(5)));
            Assert.True(SpinWait.SpinUntil(
                () => (snapshotThread.ThreadState & ThreadState.WaitSleepJoin) != 0,
                TimeSpan.FromSeconds(5)));
            Assert.Null(snapshot);

            invoice.DecidedBy = decision.Approver;
            invoice.DecidedAt = decision.DecidedAt;
            invoice.ApprovalHistory.Add(decision);
        }
        finally
        {
            Monitor.Exit(invoice);
            if (snapshotThread is not null)
            {
                Assert.True(snapshotThread.Join(TimeSpan.FromSeconds(5)));
            }
        }

        Assert.NotNull(snapshot);
        Assert.Equal(InvoiceStatus.Approved, snapshot.Status);
        Assert.Equal("demo.director", snapshot.DecidedBy);
        Assert.Equal(decision.DecidedAt, snapshot.DecidedAt);
        Assert.Equal(new[] { decision }, snapshot.ApprovalHistory);

        invoice.ApprovalHistory.Clear();
        Assert.Single(snapshot.ApprovalHistory);
    }

    [Fact]
    public void Approve_ManagerAtInclusiveLimit_SetsStatusApproverAndTimestamp()
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.PendingApproval);
        invoice.Amount = 50_000m;
        var (service, repository) = ServiceFor(invoice);

        var result = service.Approve("INV-1", "demo.manager", "Manager");

        Assert.Equal(InvoiceStatus.Approved, result.Status);
        Assert.Equal("demo.manager", result.DecidedBy);
        Assert.NotNull(result.DecidedAt);
        var decision = Assert.Single(result.ApprovalHistory);
        Assert.Equal("demo.manager", decision.Approver);
        Assert.Equal("Manager", decision.Role);
        Assert.Equal(result.DecidedAt, decision.DecidedAt);
        Assert.Equal(1, repository.UpdateCount);
    }

    [Fact]
    public void Approve_DirectorAtInclusiveLimit_SetsStatusApproverAndTimestamp()
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.PendingApproval);
        invoice.Amount = 500_000m;
        var (service, repository) = ServiceFor(invoice);

        var result = service.Approve("INV-1", "demo.director", "Director");

        Assert.Equal(InvoiceStatus.Approved, result.Status);
        Assert.Equal("demo.director", result.DecidedBy);
        Assert.NotNull(result.DecidedAt);
        var decision = Assert.Single(result.ApprovalHistory);
        Assert.Equal("demo.director", decision.Approver);
        Assert.Equal("Director", decision.Role);
        Assert.Equal(result.DecidedAt, decision.DecidedAt);
        Assert.Equal(1, repository.UpdateCount);
    }

    [Theory]
    [InlineData(50_000.01, "Manager", "Managers may approve invoices up to 50,000.")]
    public void Approve_AboveRoleLimit_LeavesInvoiceUnchangedAndDoesNotUpdateRepository(
        decimal amount, string role, string expectedReason)
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.PendingApproval);
        invoice.Amount = amount;
        invoice.DecidedBy = "previous.user";
        invoice.DecidedAt = new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        invoice.DecisionNote = "Existing decision data";
        var before = Snapshot(invoice);
        var (service, repository) = ServiceFor(invoice);

        var exception = Assert.Throws<InvalidOperationException>(
            () => service.Approve("INV-1", "demo.user", role));

        Assert.Equal(expectedReason, exception.Message);
        AssertUnchanged(before, invoice);
        Assert.Equal(0, repository.UpdateCount);
    }

    [Theory]
    [InlineData("Admin", 100)]
    [InlineData("Approver", 100)]
    [InlineData("Unknown", 100)]
    [InlineData("Admin", 500_000.01)]
    [InlineData("Approver", 500_000.01)]
    public void Approve_NonApprovalRole_LeavesInvoiceUnchangedAndDoesNotUpdateRepository(
        string role, decimal amount)
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.PendingApproval);
        invoice.Amount = amount;
        invoice.DecidedBy = "previous.user";
        invoice.DecidedAt = new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        invoice.DecisionNote = "Existing decision data";
        var before = Snapshot(invoice);
        var (service, repository) = ServiceFor(invoice);

        var exception = Assert.Throws<InvalidOperationException>(
            () => service.Approve("INV-1", "demo.user", role));

        Assert.Contains($"Role '{role}' is not permitted", exception.Message);
        AssertUnchanged(before, invoice);
        Assert.Equal(0, repository.UpdateCount);
    }

    [Theory]
    [InlineData("Manager", "demo.manager")]
    [InlineData("Director", "demo.director1")]
    public void Approve_HighValueFirstApproval_RecordsDecisionAndRemainsPending(
        string role, string approver)
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.PendingApproval);
        invoice.Amount = 500_000.01m;
        var (service, repository) = ServiceFor(invoice);

        var result = service.Approve("INV-1", approver, role);

        Assert.Equal(InvoiceStatus.PendingApproval, result.Status);
        Assert.Null(result.DecidedBy);
        Assert.Null(result.DecidedAt);
        var decision = Assert.Single(result.ApprovalHistory);
        Assert.Equal(approver, decision.Approver);
        Assert.Equal(role, decision.Role);
        Assert.True(decision.DecidedAt.Kind == DateTimeKind.Utc);
        Assert.Equal(1, repository.UpdateCount);
    }

    [Theory]
    [InlineData("Manager", "demo.manager")]
    [InlineData("Director", "demo.director1")]
    public void Approve_HighValueDistinctDirectorCompletesInDecisionOrder(
        string firstRole, string firstApprover)
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.PendingApproval);
        invoice.Amount = 500_000.01m;
        var (service, repository) = ServiceFor(invoice);

        service.Approve("INV-1", firstApprover, firstRole);
        var result = service.Approve("INV-1", "demo.director2", "Director");

        Assert.Equal(InvoiceStatus.Approved, result.Status);
        Assert.Equal("demo.director2", result.DecidedBy);
        Assert.NotNull(result.DecidedAt);
        Assert.Collection(
            result.ApprovalHistory,
            first => { Assert.Equal(firstApprover, first.Approver); Assert.Equal(firstRole, first.Role); },
            second =>
            {
                Assert.Equal("demo.director2", second.Approver);
                Assert.Equal("Director", second.Role);
                Assert.Equal(result.DecidedAt, second.DecidedAt);
            });
        Assert.True(result.ApprovalHistory[0].DecidedAt <= result.ApprovalHistory[1].DecidedAt);
        Assert.Equal(2, repository.UpdateCount);
    }

    [Fact]
    public void Approve_HighValueDuplicateSecondApprover_LeavesInvoiceAndHistoryUnchanged()
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.PendingApproval);
        invoice.Amount = 500_000.01m;
        invoice.DecidedBy = "previous.user";
        invoice.DecidedAt = new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        invoice.DecisionNote = "Existing decision data";
        var (service, repository) = ServiceFor(invoice);
        service.Approve("INV-1", "demo.director1", "Director");
        invoice.DecisionNote = "Pending-stage note";
        var before = Snapshot(invoice);

        var exception = Assert.Throws<InvalidOperationException>(
            () => service.Approve("INV-1", "DEMO.DIRECTOR1", "Director"));

        Assert.Contains("different user", exception.Message);
        AssertUnchanged(before, invoice);
        Assert.Equal(1, repository.UpdateCount);
    }

    [Fact]
    public void Approve_HighValueNonDirectorSecondApprover_LeavesInvoiceAndHistoryUnchanged()
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.PendingApproval);
        invoice.Amount = 500_000.01m;
        var (service, repository) = ServiceFor(invoice);
        service.Approve("INV-1", "demo.manager", "Manager");
        var before = Snapshot(invoice);

        var exception = Assert.Throws<InvalidOperationException>(
            () => service.Approve("INV-1", "demo.manager2", "Manager"));

        Assert.Contains("second approval", exception.Message);
        AssertUnchanged(before, invoice);
        Assert.Equal(1, repository.UpdateCount);
    }

    [Fact]
    public async Task Reject_ConcurrentWithFinalHighValueApproval_CannotOverwriteApprovedState()
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.PendingApproval);
        invoice.Amount = 500_000.01m;
        using var repository = new BlockingInvoiceRepository(invoice);
        var service = new ApprovalService(repository);

        service.Approve("INV-1", "demo.manager", "Manager");
        var finalApproval = Task.Run(() => service.Approve("INV-1", "demo.director", "Director"));
        Assert.True(repository.FinalApprovalUpdateStarted.Wait(TimeSpan.FromSeconds(5)));

        using var rejectionStarted = new ManualResetEventSlim();
        var rejection = Task.Run(() =>
        {
            rejectionStarted.Set();
            return service.Reject("INV-1", "demo.admin", "Rejected concurrently");
        });

        try
        {
            Assert.True(rejectionStarted.Wait(TimeSpan.FromSeconds(5)));
            await Task.WhenAny(rejection, Task.Delay(TimeSpan.FromMilliseconds(100)));
            Assert.False(rejection.IsCompleted);
            Assert.Equal(2, repository.UpdateCount);
        }
        finally
        {
            repository.ContinueFinalApproval();
        }

        Assert.Equal(InvoiceStatus.Approved, (await finalApproval).Status);
        var rejectionException = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await rejection);
        Assert.Contains("only pending invoices", rejectionException.Message);
        Assert.Equal(InvoiceStatus.Approved, invoice.Status);
        Assert.Equal("demo.director", invoice.DecidedBy);
        Assert.Equal(2, invoice.ApprovalHistory.Count);
        Assert.Equal(2, repository.UpdateCount);
    }

    [Theory]
    [InlineData(50_000, "Manager", "demo.manager")]
    [InlineData(500_000, "Director", "demo.director")]
    public void Approve_AtInclusiveRoleBoundary_FinalizesWithHistory(
        decimal amount, string role, string approver)
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.PendingApproval);
        invoice.Amount = amount;
        var (service, _) = ServiceFor(invoice);

        var result = service.Approve("INV-1", approver, role);

        Assert.Equal(InvoiceStatus.Approved, result.Status);
        Assert.Equal(approver, result.DecidedBy);
        Assert.Equal(approver, Assert.Single(result.ApprovalHistory).Approver);
    }

    [Theory]
    [InlineData(500_000.01, "Manager", "demo.manager")]
    [InlineData(500_000.01, "Director", "demo.director1")]
    public void Approve_HighValueFirstApproverLimitDoesNotBlockTwoApprovalPath(
        decimal amount, string role, string approver)
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.PendingApproval);
        invoice.Amount = amount;
        var (service, _) = ServiceFor(invoice);

        var result = service.Approve("INV-1", approver, role);

        Assert.Equal(InvoiceStatus.PendingApproval, result.Status);
        Assert.Equal(approver, Assert.Single(result.ApprovalHistory).Approver);
    }

    [Fact]
    public void Approve_RejectsInvoiceThatIsStillADraft()
    {
        var (service, _) = ServiceFor(InvoiceWith("INV-1", InvoiceStatus.Draft));

        Assert.Throws<InvalidOperationException>(() => service.Approve("INV-1", "demo.manager", "Manager"));
    }

    [Fact]
    public void Approve_ThrowsWhenInvoiceDoesNotExist()
    {
        var (service, _) = ServiceFor();

        Assert.Throws<KeyNotFoundException>(() => service.Approve("INV-404", "demo.manager", "Manager"));
    }

    [Fact]
    public void Reject_RecordsReasonAndApprover()
    {
        var (service, _) = ServiceFor(InvoiceWith("INV-1", InvoiceStatus.PendingApproval));

        var result = service.Reject("INV-1", "demo.approver", "Duplicate invoice");

        Assert.Equal(InvoiceStatus.Rejected, result.Status);
        Assert.Equal("Duplicate invoice", result.DecisionNote);
    }

    [Fact]
    public void Reject_RequiresAReason()
    {
        var (service, _) = ServiceFor(InvoiceWith("INV-1", InvoiceStatus.PendingApproval));

        Assert.Throws<ArgumentException>(() => service.Reject("INV-1", "demo.approver", "  "));
    }
}
