using System.Security.Claims;
using IksOks.Web.Contracts.Matches;
using IksOks.Web.Domain.Entities;
using IksOks.Web.Domain.Enums;
using IksOks.Web.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IksOks.Web.Endpoints;

public static class MatchEndpoints
{
    public static IEndpointRouteBuilder MapMatchEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/matches")
            .RequireAuthorization();

        group.MapPost("/", CreateMatchAsync);
        group.MapGet("/", GetMatchesAsync);

        return endpoints;
    }

    private static async Task<IResult> CreateMatchAsync(
        CreateMatchRequest request,
        ClaimsPrincipal principal,
        IksOksDbContext db,
        CancellationToken cancellationToken)
    {
        if (request.BoardSize < 3 || request.BoardSize > 10)
        {
            return Results.BadRequest(new
            {
                error = "Board size must be between 3 and 10."
            });
        }

        if (request.WinLength < 3 ||
            request.WinLength > request.BoardSize)
        {
            return Results.BadRequest(new
            {
                error = "Win length must be between 3 and board size."
            });
        }

        var userIdValue = principal
            .FindFirst(ClaimTypes.NameIdentifier)?
            .Value;

        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return Results.Unauthorized();
        }

        var owner = await db.Users
            .SingleOrDefaultAsync(
                user => user.Id == userId,
                cancellationToken);

        if (owner is null)
        {
            return Results.Unauthorized();
        }

        var match = new GameMatch
        {
            OwnerUserId = owner.Id,
            BoardSize = request.BoardSize,
            WinLength = request.WinLength,
            Status = MatchStatus.WaitingForOpponent
        };

        db.Matches.Add(match);

        await db.SaveChangesAsync(cancellationToken);

        return Results.Created(
            $"/api/matches/{match.Id}",
            ToResponse(match, owner.UserName));
    }

    private static async Task<IResult> GetMatchesAsync(
        IksOksDbContext db,
        CancellationToken cancellationToken)
    {
        var matches = await db.Matches
            .AsNoTracking()
            .Where(match =>
                match.Status == MatchStatus.WaitingForOpponent)
            .OrderByDescending(match => match.CreatedAt)
            .Select(match => new MatchResponse(
                match.Id,
                match.OwnerUserId,
                match.OwnerUser.UserName,
                match.OpponentUserId,
                match.OpponentUser == null
                    ? null
                    : match.OpponentUser.UserName,
                match.BoardSize,
                match.WinLength,
                match.Status.ToString(),
                match.CreatedAt))
            .ToListAsync(cancellationToken);

        return Results.Ok(matches);
    }

    private static MatchResponse ToResponse(
        GameMatch match,
        string ownerUserName)
    {
        return new MatchResponse(
            match.Id,
            match.OwnerUserId,
            ownerUserName,
            match.OpponentUserId,
            null,
            match.BoardSize,
            match.WinLength,
            match.Status.ToString(),
            match.CreatedAt);
    }
}