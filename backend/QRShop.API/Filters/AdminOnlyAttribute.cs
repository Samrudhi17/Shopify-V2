using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using QRShop.API.Services;

namespace QRShop.API.Filters;

// Admin-only endpoints. Being authenticated is not enough — the token's UID has
// to match a row in Admins. Combine with [Authorize] so an anonymous caller is
// rejected as 401 before this ever runs.
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class AdminOnlyAttribute : Attribute, IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var me = context.HttpContext.RequestServices.GetRequiredService<ICurrentUser>();

        if (!await me.IsAdminAsync())
            context.Result = new ObjectResult(new { message = "Admin access required." }) { StatusCode = 403 };
    }
}
