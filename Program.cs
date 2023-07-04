using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// 註冊 IAntiforgery 服務
builder.Services.AddAntiforgery(o => o.HeaderName = "X-XSRF-TOKEN");

builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
        .AddNegotiate();
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.UseDefaultFiles();
app.UseStaticFiles();

// 註冊 /antiforgery/token 方法寫入 XSRF-TOKEN Cookie
app.MapPost("/antiforgery/token", ([FromServices]IAntiforgery forgeryService, HttpContext context) =>
{
    var tokens = forgeryService.GetAndStoreTokens(context);
    context.Response.Cookies.Append("XSRF-TOKEN", tokens.RequestToken!,
            new CookieOptions { HttpOnly = false });
    return Results.Ok();
});

app.MapPost("/ajax", (HttpContext ctx) => 
    (ctx.User.Identity?.IsAuthenticated == true? 
        ctx.User.Identity.Name : "Anonymous") + ":" +
    Guid.NewGuid().ToString())
    .RequireAuthorization()
    .ValidateAntiforgery(); //加上 ValidateAntiforgery() 要求驗證 XSRF-TOKEN

app.Run();

// 宣告 ValidateAntiforgery() 擴充方法
internal static class AntiForgeryExtensions
{
    public static TBuilder ValidateAntiforgery<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder
    {
        return builder.AddEndpointFilter(routeHandlerFilter: async (context, next) =>
        {
            try
            {
                var antiForgeryService = context.HttpContext.RequestServices.GetRequiredService<IAntiforgery>();
                await antiForgeryService.ValidateRequestAsync(context.HttpContext);
            }
            catch (AntiforgeryValidationException)
            {
                return Results.BadRequest("Antiforgery token validation failed.");
            }
            return await next(context);
        });
    }
}
