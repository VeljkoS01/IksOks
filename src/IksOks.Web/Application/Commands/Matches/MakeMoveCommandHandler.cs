using System.Text.Json;
using IksOks.Web.Domain.Entities;
using IksOks.Web.Domain.Enums;
using IksOks.Web.Domain.States;
using IksOks.Web.Domain.Strategies;
using IksOks.Web.Infrastructure.Persistence;
using IksOks.Web.Infrastructure.Persistence.Entities;
using IksOks.Web.Messaging.Contracts;
using Microsoft.EntityFrameworkCore;
using IksOks.Web.Application.Concurrency;

namespace IksOks.Web.Application.Commands.Matches;

public sealed class MakeMoveCommandHandler
    : ICommandHandler<
        MakeMoveCommand,
        MakeMoveCommandResult>
{
    private readonly IksOksDbContext _db;
    private readonly MatchStateFactory _stateFactory;
    private readonly GameRulesStrategyFactory _strategyFactory;
    private readonly MatchOperationLock _matchOperationLock;

    public MakeMoveCommandHandler(
         IksOksDbContext db,
         MatchStateFactory stateFactory,
         GameRulesStrategyFactory strategyFactory,
         MatchOperationLock matchOperationLock)
    {
        _db = db;
        _stateFactory = stateFactory;
        _strategyFactory = strategyFactory;
        _matchOperationLock = matchOperationLock;
    }

    public async Task<MakeMoveCommandResult> HandleAsync(
        MakeMoveCommand command,
        CancellationToken cancellationToken)
    {
        using var operationLock =
            await _matchOperationLock.AcquireAsync(
                command.MatchId,
                cancellationToken);

        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                cancellationToken);

        var match = await _db.Matches
            .Include(match => match.Moves)
            .SingleOrDefaultAsync(
                match =>
                    match.Id == command.MatchId,
                cancellationToken);

        if (match is null)
        {
            return MakeMoveCommandResult.Failed(
                MakeMoveFailure.MatchNotFound);
        }

        var state =
            _stateFactory.GetState(match.Status);

        if (!state.CanMakeMove(match))
        {
            return MakeMoveCommandResult.Failed(
                MakeMoveFailure.MatchNotInProgress);
        }

        if (match.TurnDeadlineAt is not null && match.TurnDeadlineAt <= DateTimeOffset.UtcNow)
        {
            return MakeMoveCommandResult.Failed(
                MakeMoveFailure.TurnExpired);
        }

        if (match.OpponentUserId
            is not Guid opponentUserId)
        {
            return MakeMoveCommandResult.Failed(
                MakeMoveFailure.MatchNotInProgress);
        }

        if (command.UserId != match.OwnerUserId &&
            command.UserId != opponentUserId)
        {
            return MakeMoveCommandResult.Failed(
                MakeMoveFailure.Forbidden);
        }

        if (command.Row < 0 ||
            command.Row >= match.BoardSize ||
            command.Column < 0 ||
            command.Column >= match.BoardSize)
        {
            return MakeMoveCommandResult.Failed(
                MakeMoveFailure.OutsideBoard);
        }

        var existingMoves = match.Moves
            .OrderBy(move => move.MoveNumber)
            .ToList();

        var occupied = existingMoves.Any(move =>
            move.Row == command.Row &&
            move.Column == command.Column);

        if (occupied)
        {
            return MakeMoveCommandResult.Failed(
                MakeMoveFailure.Occupied);
        }

        var currentTurnUserId =
            existingMoves.Count % 2 == 0
                ? match.OwnerUserId
                : opponentUserId;

        if (currentTurnUserId != command.UserId)
        {
            return MakeMoveCommandResult.Failed(
                MakeMoveFailure.NotYourTurn);
        }

        var symbol =
            command.UserId == match.OwnerUserId
                ? "X"
                : "O";

        var move = new MatchMove
        {
            MatchId = match.Id,
            PlayerUserId = command.UserId,
            Row = command.Row,
            Column = command.Column,
            MoveNumber = existingMoves.Count + 1,
            Symbol = symbol
        };

        _db.MatchMoves.Add(move);

        var allMoves = existingMoves
            .Append(move)
            .ToList();

        var strategy =
            _strategyFactory.GetStrategy(
                match.Mode);

        if (strategy.IsWinningMove(
            allMoves,
            move,
            match.WinLength))
        {
            match.Status =
                state.OnGameFinished();

            match.WinnerUserId =
                command.UserId;

            match.FinishedAt =
                DateTimeOffset.UtcNow;
        }
        else if (
            allMoves.Count ==
            match.BoardSize * match.BoardSize)
        {
            match.Status =
                state.OnGameFinished();

            match.WinnerUserId = null;

            match.FinishedAt =
                DateTimeOffset.UtcNow;
        }

        if (match.Status == MatchStatus.InProgress)
        {
            match.TurnDeadlineAt =
                DateTimeOffset.UtcNow.AddSeconds(
                    match.TurnDurationSeconds);
        }

        var matchFinished =
            match.Status == MatchStatus.Finished;

        if (matchFinished)
        {
            var eventId = Guid.NewGuid();

            var matchFinishedEvent =
                new MatchFinishedEvent(
                    eventId,
                    match.Id,
                    match.OwnerUserId,
                    opponentUserId,
                    match.WinnerUserId,
                    match.WinnerUserId is null,
                    match.BoardSize,
                    match.WinLength,
                    match.FinishedAt!.Value);

            _db.OutboxMessages.Add(
                new OutboxMessage
                {
                    Id = eventId,
                    RoutingKey =
                        "match.finished",

                    Payload =
                        JsonSerializer.Serialize(
                            matchFinishedEvent),

                    OccurredAt =
                        match.FinishedAt.Value
                });
        }

        try
        {
            await _db.SaveChangesAsync(
                cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(
                cancellationToken);

            return MakeMoveCommandResult.Failed(
                MakeMoveFailure.Conflict);
        }

        var moveData =
            new MakeMoveData(
                move.Id,
                move.PlayerUserId,
                move.Row,
                move.Column,
                move.MoveNumber,
                move.Symbol,
                move.CreatedAt);

        return MakeMoveCommandResult.Success(
            moveData,
            matchFinished);
    }
}