using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRShop.API.Data;
using QRShop.API.DTOs;
using QRShop.API.Models.Entities;

namespace QRShop.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;

    public AuthController(AppDbContext db) => _db = db;

    // Register a user. The FIRST user ever registered becomes the Admin;
    // everyone after that is a Vendor.
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest req)
    {
        var isFirstUser = !await _db.Admins.AnyAsync();

        if (isFirstUser)
        {
            var admin = new Admin
            {
                FirebaseUid = req.FirebaseUid,
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
            FirebaseUid = req.FirebaseUid,
            AdminId = adminId,
            Name = req.Name,
            Email = req.Email,
            Phone = req.Phone,
            Address = req.Address,
            Status = "Active",
        };
        _db.Vendors.Add(vendor);
        await _db.SaveChangesAsync();
        return Ok(new { role = "Vendor", id = vendor.VendorId });
    }

    // Resolve the current user's role/profile from their Firebase UID (or email).
    [HttpGet("me")]
    public async Task<ActionResult<UserProfileResponse>> Me([FromQuery] string firebaseUid)
    {
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
