using System.Security.Claims;
using IksOks.Web.Contracts.Users;
using IksOks.Web.Domain.Enums;
using IksOks.Web.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IksOks.Web.Endpoints;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/users")
            .RequireAuthorization();

        group.MapGet(
            "/me/profile",
            GetMyProfileAsync);

        group.MapGet(
            "/leaderboard",
            GetLeaderboardAsync);

        return endpoints;
    }

    private static async Task<IResult> GetMyProfileAsync(
        ClaimsPrincipal principal,
        IksOksDbContext db,
        CancellationToken cancellationToken)
    {
        var userIdValue = principal
            .FindFirst(ClaimTypes.NameIdentifier)?
            .Value;

        if (!Guid.TryParse(
            userIdValue,
            out var userId))
        {
            return Results.Unauthorized();
        }

        var user = await db.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(
                user => user.Id == userId,
                cancellationToken);

        if (user is null)
        {
            return Results.Unauthorized();
        }

        var matchResults = await db.Matches
            .AsNoTracking()
            .Where(match =>
                match.Status == MatchStatus.Finished &&
                (
                    match.OwnerUserId == userId ||
                    match.OpponentUserId == userId
                ))
            .Select(match => match.WinnerUserId)
            .ToListAsync(cancellationToken);

        var matchesPlayed =
            matchResults.Count;

        var wins =
            matchResults.Count(
                winnerUserId =>
                    winnerUserId == userId);

        var draws =
            matchResults.Count(
                winnerUserId =>
                    winnerUserId is null);

        var losses =
            matchesPlayed - wins - draws;

        var points =
            wins * 3 + draws;

        var winRate =
            matchesPlayed == 0
                ? 0
                : Math.Round(
                    wins * 100.0 / matchesPlayed,
                    1);

        return Results.Ok(
            new UserProfileResponse(
                user.Id,
                user.UserName,
                user.CreatedAt,
                matchesPlayed,
                wins,
                draws,
                losses,
                points,
                winRate));
    }

    private static async Task<IResult> GetLeaderboardAsync(
        IksOksDbContext db,
        CancellationToken cancellationToken)
    {
        var users = await db.Users
            .AsNoTracking()
            .Select(user => new
            {
                user.Id,
                user.UserName
            })
            .ToListAsync(cancellationToken);

        var finishedMatches = await db.Matches
            .AsNoTracking()
            .Where(match =>
                match.Status == MatchStatus.Finished &&
                match.OpponentUserId != null)
            .Select(match => new
            {
                match.OwnerUserId,
                OpponentUserId =
                    match.OpponentUserId!.Value,
                match.WinnerUserId
            })
            .ToListAsync(cancellationToken);

        var entries = users
            .Select(user =>
            {
                var userMatches =
                    finishedMatches
                        .Where(match =>
                            match.OwnerUserId == user.Id ||
                            match.OpponentUserId == user.Id)
                        .ToList();

                var matchesPlayed =
                    userMatches.Count;

                var wins =
                    userMatches.Count(
                        match =>
                            match.WinnerUserId == user.Id);

                var draws =
                    userMatches.Count(
                        match =>
                            match.WinnerUserId is null);

                var losses =
                    matchesPlayed - wins - draws;

                var points =
                    wins * 3 + draws;

                var winRate =
                    matchesPlayed == 0
                        ? 0
                        : Math.Round(
                            wins * 100.0 /
                            matchesPlayed,
                            1);

                return new LeaderboardEntryResponse(
                    0,
                    user.Id,
                    user.UserName,
                    matchesPlayed,
                    wins,
                    draws,
                    losses,
                    points,
                    winRate);
            })
            .Where(entry =>
                entry.MatchesPlayed > 0)
            .OrderByDescending(
                entry => entry.Points)
            .ThenByDescending(
                entry => entry.Wins)
            .ThenByDescending(
                entry => entry.WinRate)
            .ThenBy(
                entry => entry.UserName,
                StringComparer.OrdinalIgnoreCase)
            .Take(50)
            .ToList();

        var rankedEntries =
            entries
                .Select(
                    (entry, index) =>
                        entry with
                        {
                            Rank = index + 1
                        })
                .ToList();

        return Results.Ok(rankedEntries);
    }
}