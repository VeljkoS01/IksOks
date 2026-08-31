using IksOks.Web.Contracts.Auth;
using IksOks.Web.Domain.Entities;
using IksOks.Web.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace IksOks.Web.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/auth");

        group.MapPost("/register", RegisterAsync);

        group.MapPost("/login", LoginAsync);

        group.MapGet("/me", GetCurrentUser)
            .RequireAuthorization();

        group.MapPost("/logout", LogoutAsync)
            .RequireAuthorization();

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

    private static async Task<IResult> LoginAsync(
    LoginRequest request,
    IksOksDbContext db,
    IPasswordHasher<AppUser> passwordHasher,
    HttpContext httpContext,
    CancellationToken cancellationToken)
    {
        var normalizedUserName = request.UserName
            .Trim()
            .ToUpperInvariant();

        var user = await db.Users.SingleOrDefaultAsync(
            user => user.NormalizedUserName == normalizedUserName,
            cancellationToken);

        if (user is null)
        {
            return Results.Unauthorized();
        }

        var verificationResult = passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            request.Password);

        if (verificationResult == PasswordVerificationResult.Failed)
        {
            return Results.Unauthorized();
        }

        if (verificationResult == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = passwordHasher.HashPassword(
                user,
                request.Password);

            await db.SaveChangesAsync(cancellationToken);
        }

        var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new(ClaimTypes.Name, user.UserName)
    };

        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);

        var principal = new ClaimsPrincipal(identity);

        await httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal);

        return Results.Ok(
            new LoginResponse(
                user.Id,
                user.UserName));
    }

    private static IResult GetCurrentUser(
    ClaimsPrincipal principal)
    {
        var userIdValue = principal
            .FindFirst(ClaimTypes.NameIdentifier)?
            .Value;

        var userName = principal
            .FindFirst(ClaimTypes.Name)?
            .Value;

        if (!Guid.TryParse(userIdValue, out var userId) ||
            string.IsNullOrWhiteSpace(userName))
        {
            return Results.Unauthorized();
        }

        return Results.Ok(
            new LoginResponse(
                userId,
                userName));
    }

    private static async Task LogoutAsync(
    HttpContext httpContext)
    {
        httpContext.Response.StatusCode =
            StatusCodes.Status204NoContent;

        await httpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);
    }
}