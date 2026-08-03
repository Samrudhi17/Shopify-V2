using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using QRShop.API.Data;
using QRShop.API.Models.Entities;

namespace QRShop.API.Services;

// Resolves the caller's Firebase UID — taken from the validated bearer token —
// into the Vendor or Admin row it belongs to.
//
// Controllers must use this instead of reading an id out of the query string or
// request body. A client-supplied vendorId is only a *claim* about who you are,
// and anyone can edit a URL; the UID in the token is signed by Google and is the
// only identity here that cannot be forged.
public interface ICurrentUser
{
    string? FirebaseUid { get; }
    Task<Vendor?> GetVendorAsync();
    Task<int?> GetVendorIdAsync();
    Task<bool> IsAdminAsync();
}

public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _http;
    private readonly AppDbContext _db;

    // A request usually resolves the vendor more than once (the action, then the
    // subscription filter), so cache it for the lifetime of the scope.
    private Vendor? _vendor;
    private bool _vendorLoaded;

    public CurrentUser(IHttpContextAccessor http, AppDbContext db)
    {
        _http = http;
        _db = db;
    }

    // Firebase puts the UID in `sub`, which JwtBearer maps onto NameIdentifier.
    // The same value is repeated in `user_id`, which is the fallback if that
    // inbound claim mapping is ever turned off.
    public string? FirebaseUid =>
        _http.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? _http.HttpContext?.User.FindFirstValue("user_id");

    public async Task<Vendor?> GetVendorAsync()
    {
        if (_vendorLoaded) return _vendor;
        _vendorLoaded = true;

        var uid = FirebaseUid;
        if (string.IsNullOrEmpty(uid)) return _vendor = null;

        return _vendor = await _db.Vendors.FirstOrDefaultAsync(v => v.FirebaseUid == uid);
    }

    public async Task<int?> GetVendorIdAsync() => (await GetVendorAsync())?.VendorId;

    public async Task<bool> IsAdminAsync()
    {
        var uid = FirebaseUid;
        if (string.IsNullOrEmpty(uid)) return false;
        return await _db.Admins.AnyAsync(a => a.FirebaseUid == uid);
    }
}
