using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRShop.API.Data;
using QRShop.API.Filters;
using QRShop.API.Models.Entities;
using QRShop.API.Services;

namespace QRShop.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[AdminOnly]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public AdminController(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    // GET /api/admin/stats — dashboard cards (updates as vendors/shops register).
    [HttpGet("stats")]
    public async Task<IActionResult> Stats()
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var totalVendors = await _db.Vendors.CountAsync();
        var totalShops = await _db.Shops.CountAsync();
        var activeShops = await _db.Shops.CountAsync(s => s.Status == "Active");

        // Subscription counts are driven by the clock, not by the stored label,
        // so a term that lapsed without anything running stays out of "active".
        var live = _db.Subscriptions.Where(s => s.Status != SubscriptionStatus.Expired && s.EndsAt > now);
        var payingVendors = await live.Where(s => s.Plan!.Code != PlanCodes.Trial)
                                      .Select(s => s.VendorId).Distinct().CountAsync();
        var trialVendors = await live.Where(s => s.Plan!.Code == PlanCodes.Trial)
                                     .Select(s => s.VendorId).Distinct().CountAsync();
        var expiredVendors = totalVendors - await live.Select(s => s.VendorId).Distinct().CountAsync();

        // Revenue comes from settled payments, never from plan prices — a plan's
        // price can change after the fact, what was charged cannot.
        var paid = _db.PaymentTransactions.Where(p => p.Status == PaymentStatus.Paid);
        var revenuePaise = await paid.SumAsync(p => (long?)p.AmountPaise) ?? 0;
        var revenueThisMonthPaise = await paid.Where(p => p.SettledAt >= monthStart)
                                              .SumAsync(p => (long?)p.AmountPaise) ?? 0;
        var paymentsCount = await paid.CountAsync();

        // Renewal pressure: paid terms ending within a week.
        var expiringSoon = await live.CountAsync(s => s.EndsAt <= now.AddDays(7));

        return Ok(new
        {
            totalVendors,
            totalShops,
            activeShops,
            inactiveShops = totalShops - activeShops,

            payingVendors,
            trialVendors,
            expiredVendors,
            expiringSoon,

            revenue = revenuePaise / 100m,
            revenueThisMonth = revenueThisMonthPaise / 100m,
            paymentsCount,
        });
    }

    // GET /api/admin/subscriptions — every term any vendor has held, newest
    // first, with the payment that bought it. Trials appear too, so the admin
    // sees the whole lifecycle rather than only the paid part.
    [HttpGet("subscriptions")]
    public async Task<IActionResult> Subscriptions()
    {
        var now = DateTime.UtcNow;

        var rows = await _db.Subscriptions
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new
            {
                s.SubscriptionId,
                s.VendorId,
                VendorName = s.Vendor!.Name,
                VendorEmail = s.Vendor!.Email,
                ShopName = s.Vendor!.Shops.Select(x => x.ShopName).FirstOrDefault(),

                PlanCode = s.Plan!.Code,
                PlanName = s.Plan!.Name,
                s.StartsAt,
                s.EndsAt,
                StoredStatus = s.Status,

                // The settled payment for this term, if it was a paid one.
                AmountPaise = _db.PaymentTransactions
                    .Where(p => p.SubscriptionId == s.SubscriptionId && p.Status == PaymentStatus.Paid)
                    .Select(p => (int?)p.AmountPaise).FirstOrDefault(),
                PaymentId = _db.PaymentTransactions
                    .Where(p => p.SubscriptionId == s.SubscriptionId && p.Status == PaymentStatus.Paid)
                    .Select(p => p.RazorpayPaymentId).FirstOrDefault(),
                PaidAt = _db.PaymentTransactions
                    .Where(p => p.SubscriptionId == s.SubscriptionId && p.Status == PaymentStatus.Paid)
                    .Select(p => p.SettledAt).FirstOrDefault(),
            })
            .ToListAsync();

        // Status and days-remaining are derived in memory: the clock decides, so
        // the admin never sees "Active" next to a date that has already passed.
        var result = rows.Select(r => new
        {
            r.SubscriptionId,
            r.VendorId, r.VendorName, r.VendorEmail, r.ShopName,
            r.PlanCode, r.PlanName,
            r.StartsAt, r.EndsAt,
            IsTrial = r.PlanCode == PlanCodes.Trial,
            Status = r.StoredStatus == SubscriptionStatus.Expired || r.EndsAt <= now
                ? SubscriptionStatus.Expired
                : r.StoredStatus,
            DaysRemaining = r.EndsAt <= now ? 0 : (int)Math.Ceiling((r.EndsAt - now).TotalDays),
            Amount = (r.AmountPaise ?? 0) / 100m,
            r.PaymentId,
            r.PaidAt,
        });

        return Ok(result);
    }

    // GET /api/admin/vendors — vendor list with their shop.
    [HttpGet("vendors")]
    public async Task<IActionResult> Vendors()
    {
        var vendors = await _db.Vendors
            .OrderByDescending(v => v.VendorId)
            .Select(v => new
            {
                v.VendorId, v.Name, v.Email, v.Phone, v.Status,
                ShopName = v.Shops.Select(s => s.ShopName).FirstOrDefault(),
                ShopStatus = v.Shops.Select(s => s.Status).FirstOrDefault(),
            })
            .ToListAsync();
        return Ok(vendors);
    }

    // GET /api/admin/shops — all shops with owner name.
    [HttpGet("shops")]
    public async Task<IActionResult> Shops()
    {
        var shops = await _db.Shops
            .OrderByDescending(s => s.ShopId)
            .Select(s => new
            {
                s.ShopId, s.ShopName, s.Slug, s.Phone, s.Address, s.Status,
                VendorName = s.Vendor!.Name,
            })
            .ToListAsync();

        // Computed after materialising, so it always follows PUBLIC_BASE_URL.
        var withUrls = shops.Select(s => new
        {
            s.ShopId, s.ShopName, s.Slug, s.Phone, s.Address, s.Status, s.VendorName,
            CatalogUrl = PublicUrls.Catalog(_config, s.Slug),
        });
        return Ok(withUrls);
    }

    // PUT /api/admin/shops/5/status  { "status": "Active" | "Inactive" }
    [HttpPut("shops/{id:int}/status")]
    public async Task<IActionResult> SetShopStatus(int id, [FromBody] AdminStatusRequest body)
    {
        var shop = await _db.Shops.FindAsync(id);
        if (shop is null) return NotFound(new { message = "Shop not found." });
        shop.Status = body.Status;
        await _db.SaveChangesAsync();
        return Ok(new { shop.ShopId, shop.Status });
    }

    // Vendors are deliberately not deactivable: an admin controls access by
    // activating/deactivating the vendor's SHOP (above), which is what hides the
    // public catalog. There is intentionally no vendors/{id}/status endpoint.

    // GET /api/admin/admins — for the roles & permissions settings page.
    [HttpGet("admins")]
    public async Task<IActionResult> Admins()
    {
        var admins = await _db.Admins.Select(a => new { a.AdminId, a.Name, a.Email, a.Role }).ToListAsync();
        return Ok(admins);
    }
}

public record AdminStatusRequest(string Status);
