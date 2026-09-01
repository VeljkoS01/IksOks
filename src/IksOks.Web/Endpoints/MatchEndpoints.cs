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
        group.MapPost("/{matchId:guid}/join", JoinMatchAsync);

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

    private static async Task<IResult> JoinMatchAsync(
    Guid matchId,
    ClaimsPrincipal principal,
    IksOksDbContext db,
    CancellationToken cancellationToken)
    {
        var userIdValue = principal
            .FindFirst(ClaimTypes.NameIdentifier)?
            .Value;

        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return Results.Unauthorized();
        }

        var userExists = await db.Users
            .AnyAsync(
                user => user.Id == userId,
                cancellationToken);

        if (!userExists)
        {
            return Results.Unauthorized();
        }

        var matchState = await db.Matches
            .AsNoTracking()
            .Where(match => match.Id == matchId)
            .Select(match => new
            {
                match.OwnerUserId,
                match.OpponentUserId,
                match.Status
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (matchState is null)
        {
            return Results.NotFound(new
            {
                error = "Match was not found."
            });
        }

        if (matchState.OwnerUserId == userId)
        {
            return Results.BadRequest(new
            {
                error = "You cannot join your own match."
            });
        }

        if (matchState.Status != MatchStatus.WaitingForOpponent ||
            matchState.OpponentUserId is not null)
        {
            return Results.Conflict(new
            {
                error = "Match is no longer available."
            });
        }

        var updatedRows = await db.Matches
            .Where(match =>
                match.Id == matchId &&
                match.Status == MatchStatus.WaitingForOpponent &&
                match.OpponentUserId == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(
                        match => match.OpponentUserId,
                        userId)
                    .SetProperty(
                        match => match.Status,
                        MatchStatus.InProgress),
                cancellationToken);

        if (updatedRows == 0)
        {
            return Results.Conflict(new
            {
                error = "Match is no longer available."
            });
        }

        var response = await db.Matches
            .AsNoTracking()
            .Where(match => match.Id == matchId)
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
            .SingleAsync(cancellationToken);

        return Results.Ok(response);
    }
}