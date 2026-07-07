using Microsoft.AspNetCore.Mvc;
using QRShop.API.Services;

namespace QRShop.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UploadsController : ControllerBase
{
    private readonly IFileStorageService _storage;

    public UploadsController(IFileStorageService storage) => _storage = storage;

    // POST /api/uploads?folder=products  (multipart/form-data, field name: "file")
    // Returns { url } which the client then sends with the create request.
    [HttpPost]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> Upload([FromForm] IFormFile file, [FromQuery] string folder = "misc")
    {
        // Only allow a small set of known subfolders.
        var safe = folder switch
        {
            "products" or "logos" or "certificates" or "qrcodes" => folder,
            _ => "misc",
        };

        try
        {
            var url = await _storage.SaveAsync(file, safe);
            return Ok(new { url });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
