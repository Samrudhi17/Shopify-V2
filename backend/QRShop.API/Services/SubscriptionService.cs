using Microsoft.EntityFrameworkCore;
using QRShop.API.Data;
using QRShop.API.Models.Entities;

namespace QRShop.API.Services;

public interface ISubscriptionService
{
    // The vendor's newest term, whether or not it is still running.
    Task<Subscription?> GetCurrentAsync(int vendorId);

    Task<bool> HasActiveAsync(int vendorId);

    // Turns a confirmed Razorpay payment into subscription time. Safe to call
    // more than once for the same order.
    Task<SettlementResult> SettlePaymentAsync(string orderId, string? paymentId, string? rawPayload);

    Task MarkFailedAsync(string orderId, string? rawPayload);
}

public enum SettlementResult
{
    Granted,
    AlreadySettled,
    UnknownOrder,
}

public class SubscriptionService : ISubscriptionService
{
    private readonly AppDbContext _db;
    private readonly ILogger<SubscriptionService> _log;

    public SubscriptionService(AppDbContext db, ILogger<SubscriptionService> log)
    {
        _db = db;
        _log = log;
    }

    public Task<Subscription?> GetCurrentAsync(int vendorId) =>
        _db.Subscriptions
            .Include(s => s.Plan)
            .Where(s => s.VendorId == vendorId)
            .OrderByDescending(s => s.EndsAt)
            .FirstOrDefaultAsync();

    public async Task<bool> HasActiveAsync(int vendorId)
    {
        var now = DateTime.UtcNow;
        return await _db.Subscriptions
            .AnyAsync(s => s.VendorId == vendorId
                        && s.Status != SubscriptionStatus.Expired
                        && s.EndsAt > now);
    }

    public async Task<SettlementResult> SettlePaymentAsync(string orderId, string? paymentId, string? rawPayload)
    {
        // The connection is configured with EnableRetryOnFailure, and that
        // strategy refuses to run a transaction opened directly — a retry has to
        // be able to replay the whole unit, not resume half of one. So the
        // transaction lives inside the strategy's own retry loop.
        var strategy = _db.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            // A retry replays this lambda after a rollback, and anything the
            // failed attempt left tracked would be inserted a second time.
            _db.ChangeTracker.Clear();

            return await SettleOnceAsync(orderId, paymentId, rawPayload);
        });
    }

    private async Task<SettlementResult> SettleOnceAsync(string orderId, string? paymentId, string? rawPayload)
    {
        // The browser callback and the webhook both settle the same order and can
        // arrive at once. SELECT ... FOR UPDATE makes the second caller wait for
        // the first to commit, so it sees the settled row instead of granting a
        // second term for one payment.
        await using var tx = await _db.Database.BeginTransactionAsync();

        var txn = await _db.PaymentTransactions
            .FromSql($"SELECT * FROM Payment_Transactions WHERE RazorpayOrderId = {orderId} FOR UPDATE")
            .FirstOrDefaultAsync();

        if (txn is null)
        {
            // An order this API never created — ignore rather than invent a term.
            _log.LogWarning("Settlement for unknown Razorpay order {OrderId}.", orderId);
            return SettlementResult.UnknownOrder;
        }

        if (txn.SubscriptionId is not null)
            return SettlementResult.AlreadySettled;

        var plan = await _db.Plans.FirstAsync(p => p.PlanId == txn.PlanId);
        var now = DateTime.UtcNow;

        // Renewing early adds to the remaining time instead of discarding it, so
        // a vendor is never punished for paying before the last day.
        var current = await GetCurrentAsync(txn.VendorId);
        var startsAt = current is not null && current.EndsAt > now ? current.EndsAt : now;

        var subscription = new Subscription
        {
            VendorId = txn.VendorId,
            PlanId = plan.PlanId,
            StartsAt = startsAt,
            EndsAt = startsAt.AddDays(plan.DurationDays),
            Status = SubscriptionStatus.Active,
        };
        _db.Subscriptions.Add(subscription);

        txn.Status = PaymentStatus.Paid;
        txn.RazorpayPaymentId = paymentId ?? txn.RazorpayPaymentId;
        txn.RawPayload = rawPayload ?? txn.RawPayload;
        txn.SettledAt = now;
        txn.Subscription = subscription;

        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        _log.LogInformation(
            "Granted {PlanCode} to vendor {VendorId} until {EndsAt:u} (order {OrderId}).",
            plan.Code, txn.VendorId, subscription.EndsAt, orderId);

        return SettlementResult.Granted;
    }

    public async Task MarkFailedAsync(string orderId, string? rawPayload)
    {
        var txn = await _db.PaymentTransactions.FirstOrDefaultAsync(t => t.RazorpayOrderId == orderId);

        // Never downgrade a settled payment: a failed attempt can be reported
        // after a later successful one on the same order.
        if (txn is null || txn.SubscriptionId is not null) return;

        txn.Status = PaymentStatus.Failed;
        txn.RawPayload = rawPayload ?? txn.RawPayload;
        await _db.SaveChangesAsync();
    }
}
