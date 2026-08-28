using IksOks.Web.Contracts.Auth;
using IksOks.Web.Domain.Entities;
using IksOks.Web.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace IksOks.Web.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/auth");

        group.MapPost("/register", RegisterAsync);

        return endpoints;
    }

    private static async Task<IResult> RegisterAsync(
        RegisterRequest request,
        IksOksDbContext db,
        IPasswordHasher<AppUser> passwordHasher,
        CancellationToken cancellationToken)
    {
        var userName = request.UserName.Trim();

        if (userName.Length < 3 || userName.Length > 32)
        {
            return Results.BadRequest(new
            {
                error = "Username must contain between 3 and 32 characters."
            });
        }

        if (userName.Any(char.IsWhiteSpace))
        {
            return Results.BadRequest(new
            {
                error = "Username cannot contain whitespace."
            });
        }

        if (request.Password.Length < 8 || request.Password.Length > 128)
        {
            return Results.BadRequest(new
            {
                error = "Password must contain between 8 and 128 characters."
            });
        }

        var normalizedUserName = userName.ToUpperInvariant();

        var userExists = await db.Users.AnyAsync(
            user => user.NormalizedUserName == normalizedUserName,
            cancellationToken);

        if (userExists)
        {
            return Results.Conflict(new
            {
                error = "Username is already taken."
            });
        }

        var user = new AppUser
        {
            UserName = userName,
            NormalizedUserName = normalizedUserName
        };

        user.PasswordHash = passwordHasher.HashPassword(
            user,
            request.Password);

        db.Users.Add(user);

        await db.SaveChangesAsync(cancellationToken);

        var response = new RegisterResponse(
            user.Id,
            user.UserName);

        return Results.Created(
            $"/api/users/{user.Id}",
            response);
    }
}