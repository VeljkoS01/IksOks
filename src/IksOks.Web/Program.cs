using IksOks.Web.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using IksOks.Web.Domain.Entities;
using IksOks.Web.Endpoints;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<IksOksDbContext>(options =>
{
    var connectionString =
        builder.Configuration.GetConnectionString("Default");

    options.UseNpgsql(connectionString);
});

builder.Services.AddScoped<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();

var app = builder.Build();

app.MapGet("/api/health", () =>
{
    return Results.Ok(new
    {
        status = "ok",
        application = "IksOks"
    });
});

app.MapAuthEndpoints();

app.Run();
