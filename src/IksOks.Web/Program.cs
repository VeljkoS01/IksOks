using IksOks.Web.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using IksOks.Web.Domain.Entities;
using IksOks.Web.Endpoints;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<IksOksDbContext>(options =>
{
    var connectionString =
        builder.Configuration.GetConnectionString("Default");

    options.UseNpgsql(connectionString);
});

builder.Services.AddScoped<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "IksOks.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;

        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };

        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/health", () =>
{
    return Results.Ok(new
    {
        status = "ok",
        application = "IksOks"
    });
});

app.UseAuthentication();
app.UseAuthorization();
app.MapAuthEndpoints();
app.MapMatchEndpoints();

app.Run();
