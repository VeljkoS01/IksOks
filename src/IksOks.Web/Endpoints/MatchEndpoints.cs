using System.Security.Claims;
using IksOks.Web.Contracts.Matches;
using IksOks.Web.Domain.Entities;
using IksOks.Web.Domain.Enums;
using IksOks.Web.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using IksOks.Web.Domain.Services;
using IksOks.Web.Realtime;
using Microsoft.AspNetCore.SignalR;
using IksOks.Web.Messaging;
using IksOks.Web.Messaging.Contracts;

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
        group.MapGet("/{matchId:guid}", GetMatchAsync);
        group.MapPost("/{matchId:guid}/moves",MakeMoveAsync);
        group.MapGet("/mine/active",GetMyActiveMatchesAsync);
        group.MapGet("/mine/history",GetMyMatchHistoryAsync);

        return endpoints;
    }

    private static async Task<IResult> CreateMatchAsync(
        CreateMatchRequest request,
        ClaimsPrincipal principal,
        IksOksDbContext db,
        IHubContext<MatchHub> hub,
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

        await hub.Clients.All.SendAsync("LobbyUpdated");

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
        IHubContext<MatchHub> hub,
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

        await hub.Clients
            .Group(MatchHub.GroupName(matchId))
            .SendAsync(
                "MatchUpdated",
                matchId);

        await hub.Clients.All
            .SendAsync("LobbyUpdated");

        return Results.Ok(response);
    }

    private static async Task<IResult> GetMatchAsync(
    Guid matchId,
    IksOksDbContext db,
    CancellationToken cancellationToken)
    {
        var match = await db.Matches
            .AsNoTracking()
            .Include(match => match.OwnerUser)
            .Include(match => match.OpponentUser)
            .Include(match => match.WinnerUser)
            .Include(match => match.Moves)
            .SingleOrDefaultAsync(
                match => match.Id == matchId,
                cancellationToken);

        if (match is null)
        {
            return Results.NotFound(new
            {
                error = "Match was not found."
            });
        }

        return Results.Ok(ToDetailsResponse(match));
    }

    private static async Task<IResult> MakeMoveAsync(
        Guid matchId,
        MakeMoveRequest request,
        ClaimsPrincipal principal,
        IksOksDbContext db,
        IHubContext<MatchHub> hub,
        IEventPublisher eventPublisher,
        CancellationToken cancellationToken)
    {
        var userIdValue = principal
            .FindFirst(ClaimTypes.NameIdentifier)?
            .Value;

        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return Results.Unauthorized();
        }

        await using var transaction =
            await db.Database.BeginTransactionAsync(
                cancellationToken);

        var match = await db.Matches
            .Include(match => match.Moves)
            .SingleOrDefaultAsync(
                match => match.Id == matchId,
                cancellationToken);

        if (match is null)
        {
            return Results.NotFound(new
            {
                error = "Match was not found."
            });
        }

        if (match.Status != MatchStatus.InProgress ||
            match.OpponentUserId is null)
        {
            return Results.Conflict(new
            {
                error = "Match is not in progress."
            });
        }

        if (userId != match.OwnerUserId &&
            userId != match.OpponentUserId)
        {
            return Results.Forbid();
        }

        if (request.Row < 0 ||
            request.Row >= match.BoardSize ||
            request.Column < 0 ||
            request.Column >= match.BoardSize)
        {
            return Results.BadRequest(new
            {
                error = "Move is outside of the board."
            });
        }

        var existingMoves = match.Moves
            .OrderBy(move => move.MoveNumber)
            .ToList();

        var occupied = existingMoves.Any(move =>
            move.Row == request.Row &&
            move.Column == request.Column);

        if (occupied)
        {
            return Results.Conflict(new
            {
                error = "Field is already occupied."
            });
        }

        var currentTurnUserId =
            existingMoves.Count % 2 == 0
                ? match.OwnerUserId
                : match.OpponentUserId.Value;

        if (currentTurnUserId != userId)
        {
            return Results.Conflict(new
            {
                error = "It is not your turn."
            });
        }

        var symbol =
            userId == match.OwnerUserId
                ? "X"
                : "O";

        var move = new MatchMove
        {
            MatchId = match.Id,
            PlayerUserId = userId,
            Row = request.Row,
            Column = request.Column,
            MoveNumber = existingMoves.Count + 1,
            Symbol = symbol
        };

        db.MatchMoves.Add(move);

        var allMoves = existingMoves
            .Append(move)
            .ToList();

        if (GameRules.IsWinningMove(
            allMoves,
            move,
            match.WinLength))
        {
            match.Status = MatchStatus.Finished;
            match.WinnerUserId = userId;
            match.FinishedAt = DateTimeOffset.UtcNow;
        }
        else if (allMoves.Count ==
                 match.BoardSize * match.BoardSize)
        {
            match.Status = MatchStatus.Finished;
            match.WinnerUserId = null;
            match.FinishedAt = DateTimeOffset.UtcNow;
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            if (match.Status == MatchStatus.Finished)
            {
                var matchFinishedEvent =
                    new MatchFinishedEvent(
                        Guid.NewGuid(),
                        match.Id,
                        match.OwnerUserId,
                        match.OpponentUserId!.Value,
                        match.WinnerUserId,
                        match.WinnerUserId is null,
                        match.BoardSize,
                        match.WinLength,
                        match.FinishedAt!.Value);

                await eventPublisher.PublishMatchFinishedAsync(
                    matchFinishedEvent,
                    cancellationToken);
            }

            await hub.Clients
                .Group(MatchHub.GroupName(matchId))
                .SendAsync(
                    "MatchUpdated",
                    matchId);

            if (match.Status == MatchStatus.Finished)
            {
                await hub.Clients.All
                    .SendAsync("LobbyUpdated");
            }
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);

            return Results.Conflict(new
            {
                error = "Move could not be completed."
            });
        }

        return Results.Ok(
            new MoveResponse(
                move.Id,
                move.PlayerUserId,
                move.Row,
                move.Column,
                move.MoveNumber,
                move.Symbol,
                move.CreatedAt));
    }

    private static MatchDetailsResponse ToDetailsResponse(
    GameMatch match)
    {
        Guid? currentTurnUserId = null;

        if (match.Status == MatchStatus.InProgress &&
            match.OpponentUserId is not null)
        {
            currentTurnUserId =
                match.Moves.Count % 2 == 0
                    ? match.OwnerUserId
                    : match.OpponentUserId;
        }

        var moves = match.Moves
            .OrderBy(move => move.MoveNumber)
            .Select(move => new MoveResponse(
                move.Id,
                move.PlayerUserId,
                move.Row,
                move.Column,
                move.MoveNumber,
                move.Symbol,
                move.CreatedAt))
            .ToList();

        return new MatchDetailsResponse(
            match.Id,
            match.OwnerUserId,
            match.OwnerUser.UserName,
            match.OpponentUserId,
            match.OpponentUser?.UserName,
            match.BoardSize,
            match.WinLength,
            match.Status.ToString(),
            currentTurnUserId,
            match.WinnerUserId,
            match.WinnerUser?.UserName,
            match.CreatedAt,
            match.FinishedAt,
            moves);
    }

    private static async Task<IResult> GetMyActiveMatchesAsync(
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

        var matches = await db.Matches
            .AsNoTracking()
            .Where(match =>
                (
                    match.OwnerUserId == userId ||
                    match.OpponentUserId == userId
                ) &&
                (
                    match.Status ==
                        MatchStatus.WaitingForOpponent ||
                    match.Status ==
                        MatchStatus.InProgress
                ))
            .OrderByDescending(match => match.CreatedAt)
            .Select(match => new UserMatchResponse(
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
                match.WinnerUserId,
                match.WinnerUser == null
                    ? null
                    : match.WinnerUser.UserName,
                match.CreatedAt,
                match.FinishedAt))
            .ToListAsync(cancellationToken);

        return Results.Ok(matches);
    }
    private static async Task<IResult> GetMyMatchHistoryAsync(
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

        var matches = await db.Matches
            .AsNoTracking()
            .Where(match =>
                (
                    match.OwnerUserId == userId ||
                    match.OpponentUserId == userId
                ) &&
                match.Status == MatchStatus.Finished)
            .OrderByDescending(match => match.FinishedAt)
            .Take(50)
            .Select(match => new UserMatchResponse(
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
                match.WinnerUserId,
                match.WinnerUser == null
                    ? null
                    : match.WinnerUser.UserName,
                match.CreatedAt,
                match.FinishedAt))
            .ToListAsync(cancellationToken);

        return Results.Ok(matches);
    }
}