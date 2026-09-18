using System.Security.Claims;
using IksOks.Web.Contracts.Matches;
using IksOks.Web.Domain.Entities;
using IksOks.Web.Domain.Enums;
using IksOks.Web.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using IksOks.Web.Realtime;
using Microsoft.AspNetCore.SignalR;
using IksOks.Web.Domain.Strategies;
using IksOks.Web.Domain.States;
using IksOks.Web.Application.Commands;
using IksOks.Web.Application.Commands.Matches;

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
        group.MapGet("/live", GetLiveMatchesAsync);
        group.MapPost("/{matchId:guid}/pause-request",RequestPauseAsync);
        group.MapPost("/{matchId:guid}/pause",PauseMatchAsync);
        group.MapPost("/{matchId:guid}/pause-request/reject",RejectPauseRequestAsync);
        group.MapPost("/{matchId:guid}/resume",ResumeMatchAsync);
        group.MapPost("/{matchId:guid}/resume-request", RequestResumeAsync);
        group.MapPost("/{matchId:guid}/resume-request/reject", RejectResumeRequestAsync);

        return endpoints;
    }

    private static async Task<IResult> CreateMatchAsync(
        CreateMatchRequest request,
        ClaimsPrincipal principal,
        IksOksDbContext db,
        GameRulesStrategyFactory strategyFactory,
        IHubContext<MatchHub> hub,
        CancellationToken cancellationToken)
    {

        if (!Enum.TryParse<MatchMode>(
            request.Mode,
            ignoreCase: true,
            out var mode))
        {
            return Results.BadRequest(new
            {
                error = "Unknown match mode."
            });
        }

        var strategy = strategyFactory.GetStrategy(mode);

        if (!strategy.IsValidConfiguration(
            request.BoardSize,
            request.WinLength))
        {
            return Results.BadRequest(new
            {
                error =
                    "Configuration is not valid for the selected match mode."
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
            Mode = mode,
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
                match.Mode.ToString(),
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
            match.Mode.ToString(),
            match.BoardSize,
            match.WinLength,
            match.Status.ToString(),
            match.CreatedAt);
    }

    private static async Task<IResult> JoinMatchAsync(
        Guid matchId,
        ClaimsPrincipal principal,
        IksOksDbContext db,
        MatchStateFactory stateFactory,
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

        var match = await db.Matches
            .AsNoTracking()
            .SingleOrDefaultAsync(match => match.Id == matchId, cancellationToken);

        if (match is null)
        {
            return Results.NotFound(new
            {
                error = "Match was not found."
            });
        }

        if (match is null)
        {
            return Results.NotFound(new
            {
                error = "Match was not found."
            });
        }

        if (match.OwnerUserId == userId)
        {
            return Results.BadRequest(new
            {
                error = "You cannot join your own match."
            });
        }

        var state = stateFactory.GetState(match.Status);
        if (!state.CanJoin(match))
        {
            return Results.Conflict(new
            {
                error = "Match is no longer available."
            });
        }

        var nextStatus = state.OnOpponentJoined();

        var firstTurnDeadline = DateTimeOffset.UtcNow.AddSeconds(30);

        var updatedRows = await db.Matches
            .Where(match =>
                match.Id == matchId &&
                match.Status == state.Status &&
                match.OpponentUserId == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(
                        match => match.OpponentUserId,
                        userId)
                    .SetProperty(
                        match => match.Status,
                        nextStatus)
                    .SetProperty(
                        match => match.TurnDeadlineAt,
                        firstTurnDeadline),
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
                match.Mode.ToString(),
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
            .Include(match => match.PauseRequestedByUser)
            .Include(match => match.ResumeRequestedByUser)
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
    ICommandHandler<
        MakeMoveCommand,
        MakeMoveCommandResult> handler,
    IHubContext<MatchHub> hub,
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

        var command =
            new MakeMoveCommand(
                matchId,
                userId,
                request.Row,
                request.Column);

        var result =
            await handler.HandleAsync(
                command,
                cancellationToken);

        if (!result.IsSuccess)
        {
            return result.Failure switch
            {
                MakeMoveFailure.MatchNotFound =>
                    Results.NotFound(new
                    {
                        error =
                            "Match was not found."
                    }),

                MakeMoveFailure.MatchNotInProgress =>
                    Results.Conflict(new
                    {
                        error =
                            "Match is not in progress."
                    }),

                MakeMoveFailure.Forbidden =>
                    Results.Forbid(),

                MakeMoveFailure.OutsideBoard =>
                    Results.BadRequest(new
                    {
                        error =
                            "Move is outside of the board."
                    }),

                MakeMoveFailure.Occupied =>
                    Results.Conflict(new
                    {
                        error =
                            "Field is already occupied."
                    }),

                MakeMoveFailure.NotYourTurn =>
                    Results.Conflict(new
                    {
                        error =
                            "It is not your turn."
                    }),

                _ =>
                    Results.Conflict(new
                    {
                        error =
                            "Move could not be completed."
                    })
            };
        }

        var move = result.Move!;

        await hub.Clients
            .Group(MatchHub.GroupName(matchId))
            .SendAsync(
                "MatchUpdated",
                matchId,
                cancellationToken);

        if (result.MatchFinished)
        {
            await hub.Clients.All
                .SendAsync(
                    "LobbyUpdated",
                    cancellationToken);
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
            match.Mode.ToString(),
            match.BoardSize,
            match.WinLength,
            match.Status.ToString(),
            match.PauseRequestedByUserId,
            match.PauseRequestedByUser?.UserName,
            match.PauseRequestedAt,
            match.ResumeRequestedByUserId,
            match.ResumeRequestedByUser?.UserName,
            match.ResumeRequestedAt,
            match.TurnDurationSeconds,
            match.TurnDeadlineAt,
            currentTurnUserId,
            match.WinnerUserId,
            match.WinnerUser?.UserName,
            match.CreatedAt,
            match.FinishedAt,
            moves);
    }

    private static async Task<IResult> GetLiveMatchesAsync(
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

        var matches = await db.Matches
            .AsNoTracking()
            .Where(match =>
                match.OpponentUserId != null &&
                (
                    match.Status == MatchStatus.InProgress ||
                    match.Status == MatchStatus.Paused
                ) &&
                match.OwnerUserId != userId &&
                match.OpponentUserId != userId)
            .OrderByDescending(
                match => match.CreatedAt)
            .Select(match =>
                new LiveMatchResponse(
                    match.Id,
                    match.OwnerUserId,
                    match.OwnerUser.UserName,
                    match.OpponentUserId!.Value,
                    match.OpponentUser!.UserName,
                    match.Mode.ToString(),
                    match.BoardSize,
                    match.WinLength,
                    match.Status.ToString(),
                    match.CreatedAt))
            .ToListAsync(cancellationToken);

        return Results.Ok(matches);
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
                    match.Status == MatchStatus.WaitingForOpponent ||
                    match.Status == MatchStatus.InProgress ||
                    match.Status == MatchStatus.Paused
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
                match.Mode.ToString(),
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
                match.Mode.ToString(),
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

    private static async Task NotifyMatchChangedAsync(
    Guid matchId,
    IHubContext<MatchHub> hub,
    CancellationToken cancellationToken)
    {
        await hub.Clients
            .Group(MatchHub.GroupName(matchId))
            .SendAsync(
                "MatchUpdated",
                matchId,
                cancellationToken);

        await hub.Clients.All
            .SendAsync(
                "LobbyUpdated",
                cancellationToken);
    }

    private static IResult ToMatchControlFailureResult(
    MatchControlFailure? failure)
    {
        return failure switch
        {
            MatchControlFailure.MatchNotFound =>
                Results.NotFound(new
                {
                    error = "Match was not found."
                }),

            MatchControlFailure.Forbidden =>
                Results.Forbid(),

            MatchControlFailure
                .PauseRequestAlreadyExists =>
                Results.Conflict(new
                {
                    error =
                        "A pause request already exists."
                }),

            MatchControlFailure
                .PauseRequestNotFound =>
                Results.Conflict(new
                {
                    error =
                        "There is no pause request."
                }),

            _ =>
                Results.Conflict(new
                {
                    error =
                        "This action is not allowed in the current match state."
                })
        };
    }

    private static async Task<IResult> RequestPauseAsync(
    Guid matchId,
    ClaimsPrincipal principal,
    ICommandHandler<
        RequestPauseCommand,
        MatchControlCommandResult> handler,
    IHubContext<MatchHub> hub,
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

        var result =
            await handler.HandleAsync(
                new RequestPauseCommand(
                    matchId,
                    userId),
                cancellationToken);

        if (!result.IsSuccess)
        {
            return ToMatchControlFailureResult(
                result.Failure);
        }

        await NotifyMatchChangedAsync(
            matchId,
            hub,
            cancellationToken);

        return Results.NoContent();
    }


    private static async Task<IResult> PauseMatchAsync(
    Guid matchId,
    ClaimsPrincipal principal,
    ICommandHandler<
        PauseMatchCommand,
        MatchControlCommandResult> handler,
    IHubContext<MatchHub> hub,
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

        var result =
            await handler.HandleAsync(
                new PauseMatchCommand(
                    matchId,
                    userId),
                cancellationToken);

        if (!result.IsSuccess)
        {
            return ToMatchControlFailureResult(
                result.Failure);
        }

        await NotifyMatchChangedAsync(
            matchId,
            hub,
            cancellationToken);

        return Results.NoContent();
    }

    private static async Task<IResult> RejectPauseRequestAsync(
    Guid matchId,
    ClaimsPrincipal principal,
    ICommandHandler<
        RejectPauseRequestCommand,
        MatchControlCommandResult> handler,
    IHubContext<MatchHub> hub,
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

        var result =
            await handler.HandleAsync(
                new RejectPauseRequestCommand(
                    matchId,
                    userId),
                cancellationToken);

        if (!result.IsSuccess)
        {
            return ToMatchControlFailureResult(
                result.Failure);
        }

        await NotifyMatchChangedAsync(
            matchId,
            hub,
            cancellationToken);

        return Results.NoContent();
    }
    private static async Task<IResult> ResumeMatchAsync(
    Guid matchId,
    ClaimsPrincipal principal,
    ICommandHandler<
        ResumeMatchCommand,
        MatchControlCommandResult> handler,
    IHubContext<MatchHub> hub,
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

        var result =
            await handler.HandleAsync(
                new ResumeMatchCommand(
                    matchId,
                    userId),
                cancellationToken);

        if (!result.IsSuccess)
        {
            return ToMatchControlFailureResult(
                result.Failure);
        }

        await NotifyMatchChangedAsync(
            matchId,
            hub,
            cancellationToken);

        return Results.NoContent();
    }

    private static async Task<IResult> RequestResumeAsync(
    Guid matchId,
    ClaimsPrincipal principal,
    ICommandHandler<
        RequestResumeCommand,
        MatchControlCommandResult> handler,
    IHubContext<MatchHub> hub,
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

        var result = await handler.HandleAsync(
            new RequestResumeCommand(
                matchId,
                userId),
            cancellationToken);

        if (!result.IsSuccess)
        {
            return ToMatchControlFailureResult(
                result.Failure);
        }

        await NotifyMatchChangedAsync(
            matchId,
            hub,
            cancellationToken);

        return Results.NoContent();
    }

    private static async Task<IResult> RejectResumeRequestAsync(
    Guid matchId,
    ClaimsPrincipal principal,
    ICommandHandler<
        RejectResumeRequestCommand,
        MatchControlCommandResult> handler,
    IHubContext<MatchHub> hub,
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

        var result = await handler.HandleAsync(
            new RejectResumeRequestCommand(
                matchId,
                userId),
            cancellationToken);

        if (!result.IsSuccess)
        {
            return ToMatchControlFailureResult(
                result.Failure);
        }

        await NotifyMatchChangedAsync(
            matchId,
            hub,
            cancellationToken);

        return Results.NoContent();
    }
}