using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRShop.API.Data;
using QRShop.API.DTOs;
using QRShop.API.Models.Entities;
using QRShop.API.Services;

namespace QRShop.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _me;

    public AuthController(AppDbContext db, ICurrentUser me)
    {
        _db = db;
        _me = me;
    }

    // Register a user. The FIRST user ever registered becomes the Admin;
    // everyone after that is a Vendor.
    //
    // The client has already created the Firebase account by the time it calls
    // this, so it is authenticated. The UID comes from the token rather than
    // req.FirebaseUid — otherwise anyone could create a row pointing at somebody
    // else's Firebase account and then sign in as them.
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest req)
    {
        var firebaseUid = _me.FirebaseUid;
        if (string.IsNullOrEmpty(firebaseUid))
            return Unauthorized(new { message = "Sign in before registering a profile." });

        // Registering twice with the same account would otherwise create a
        // duplicate vendor row that /auth/me resolves arbitrarily.
        if (await _db.Vendors.AnyAsync(v => v.FirebaseUid == firebaseUid)
            || await _db.Admins.AnyAsync(a => a.FirebaseUid == firebaseUid))
            return Conflict(new { message = "This account is already registered." });

        var isFirstUser = !await _db.Admins.AnyAsync();

        if (isFirstUser)
        {
            var admin = new Admin
            {
                FirebaseUid = firebaseUid,
                Name = req.Name,
                Email = req.Email,
                Role = "Admin",
            };
            _db.Admins.Add(admin);
            await _db.SaveChangesAsync();
            return Ok(new { role = "Admin", id = admin.AdminId });
        }

        if (await _db.Vendors.AnyAsync(v => v.Email == req.Email))
            return Conflict(new { message = "A vendor with this email already exists." });

        var adminId = await _db.Admins.Select(a => (int?)a.AdminId).FirstOrDefaultAsync();

        var vendor = new Vendor
        {
            FirebaseUid = firebaseUid,
            AdminId = adminId,
            Name = req.Name,
            Email = req.Email,
            Phone = req.Phone,
            Address = req.Address,
            Status = "Active",
        };
        _db.Vendors.Add(vendor);

        // Start the free trial immediately. Without it the vendor would land on a
        // dashboard that is already gated, since access is decided purely by
        // whether a subscription term is still running.
        //
        // Saved in the same SaveChangesAsync as the vendor — one transaction, so
        // a failure here cannot leave a vendor with no trial. The subscription
        // points at `vendor` by navigation property because VendorId does not
        // exist until the insert happens.
        var trialPlan = await _db.Plans.FirstAsync(p => p.Code == PlanCodes.Trial);
        var now = DateTime.UtcNow;

        _db.Subscriptions.Add(new Subscription
        {
            Vendor = vendor,
            PlanId = trialPlan.PlanId,
            StartsAt = now,
            EndsAt = now.AddDays(trialPlan.DurationDays),
            Status = SubscriptionStatus.Trialing,
        });

        await _db.SaveChangesAsync();

        return Ok(new { role = "Vendor", id = vendor.VendorId });
    }

    // Resolve the current user's role/profile. The UID is read from the bearer
    // token; the ?firebaseUid= the client still sends is ignored, so asking for
    // somebody else's profile is not possible.
    [HttpGet("me")]
    public async Task<ActionResult<UserProfileResponse>> Me()
    {
        var firebaseUid = _me.FirebaseUid;

        var vendor = await _db.Vendors
            .Include(v => v.Shops)
            .FirstOrDefaultAsync(v => v.FirebaseUid == firebaseUid);

        if (vendor is not null)
        {
            return new UserProfileResponse(
                vendor.VendorId, vendor.Name, vendor.Email, "Vendor",
                vendor.Shops.FirstOrDefault()?.ShopName);
        }

        var admin = await _db.Admins.FirstOrDefaultAsync(a => a.FirebaseUid == firebaseUid);
        if (admin is not null)
            return new UserProfileResponse(admin.AdminId, admin.Name, admin.Email, "Admin", null);

        return NotFound(new { message = "User not found." });
    }
}
