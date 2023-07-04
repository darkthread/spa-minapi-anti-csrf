using Microsoft.AspNetCore.Authentication.Negotiate;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
        .AddNegotiate();
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapPost("/ajax", (HttpContext ctx) => 
    (ctx.User.Identity?.IsAuthenticated == true? 
        ctx.User.Identity.Name : "Anonymous") + ":" +
    Guid.NewGuid().ToString())
    .RequireAuthorization();

app.Run();
