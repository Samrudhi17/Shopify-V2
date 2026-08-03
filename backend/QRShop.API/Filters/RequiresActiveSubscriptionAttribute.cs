using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using QRShop.API.Services;

namespace QRShop.API.Filters;

// Blocks an action unless the calling vendor's subscription is still running.
//
// Returns 402 Payment Required so the client can tell "your plan lapsed" apart
// from "you are not signed in" (401) and "this is not yours" (403), and route
// the vendor to the pricing page instead of the login screen.
//
// Admins are exempt: they are not vendors and have no subscription of their own.
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequiresActiveSubscriptionAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var services = context.HttpContext.RequestServices;
        var me = services.GetRequiredService<ICurrentUser>();
        var subscriptions = services.GetRequiredService<ISubscriptionService>();

        var vendorId = await me.GetVendorIdAsync();

        if (vendorId is null)
        {
            if (await me.IsAdminAsync())
            {
                await next();
                return;
            }

            context.Result = new ObjectResult(new { message = "No vendor profile for this account." })
            {
                StatusCode = 404,
            };
            return;
        }

        if (!await subscriptions.HasActiveAsync(vendorId.Value))
        {
            context.Result = new ObjectResult(new
            {
                message = "Your subscription has ended. Renew your plan to continue.",
                subscriptionExpired = true,
            })
            {
                StatusCode = 402,
            };
            return;
        }

        await next();
    }
}
