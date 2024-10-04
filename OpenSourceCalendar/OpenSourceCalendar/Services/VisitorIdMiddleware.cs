namespace CasaAdelia.Middleware;

public class VisitorIdMiddleware
{
    private readonly RequestDelegate _next;

    public VisitorIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Cookies.ContainsKey("VisitorId"))
        {
            var visitorId = Guid.NewGuid().ToString();
            context.Response.Cookies.Append("VisitorId", visitorId, new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) });
        }

        await _next(context);
    }
}
