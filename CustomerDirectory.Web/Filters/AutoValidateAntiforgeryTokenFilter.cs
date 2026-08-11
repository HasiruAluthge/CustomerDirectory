using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CustomerDirectory.Web.Filters;

public class AutoValidateAntiforgeryTokenFilter : IAsyncAuthorizationFilter
{
    private readonly IAntiforgery _antiforgery;
    public AutoValidateAntiforgeryTokenFilter(IAntiforgery antiforgery) => _antiforgery = antiforgery;

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var method = context.HttpContext.Request.Method;
        if (HttpMethods.IsGet(method) || HttpMethods.IsHead(method)) return; // safe methods, skip

        try
        {
            await _antiforgery.ValidateRequestAsync(context.HttpContext);
        }
        catch (AntiforgeryValidationException)
        {
            context.Result = new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(
                new { message = "Invalid or missing anti-forgery token." });
        }
    }
}