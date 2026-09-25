using IksOks.Web.Application.Concurrency;
using IksOks.Web.Domain.Entities;
using IksOks.Web.Domain.States;
using IksOks.Web.Infrastructure.Persistence;
using IksOks.Web.Realtime.Collaboration;
using Microsoft.EntityFrameworkCore;

namespace IksOks.Web.Application.Commands.Matches;

public sealed class MatchControlCommandHandler
    : ICommandHandler<
        RequestPauseCommand,
        MatchControlCommandResult>,
      ICommandHandler<
        PauseMatchCommand,
        MatchControlCommandResult>,
      ICommandHandler<
        RejectPauseRequestCommand,
        MatchControlCommandResult>,
      ICommandHandler<
        ResumeMatchCommand,
        MatchControlCommandResult>,
      ICommandHandler<
        RequestResumeCommand,
        MatchControlCommandResult>,
      ICommandHandler<
        RejectResumeRequestCommand,
        MatchControlCommandResult>
{
    private readonly IksOksDbContext _db;
    private readonly MatchStateFactory _stateFactory;
    private readonly MatchOperationLock _matchOperationLock;
    private readonly MatchControlRegistry _controlRegistry;

    public MatchControlCommandHandler(
        IksOksDbContext db,
        MatchStateFactory stateFactory,
        MatchOperationLock matchOperationLock,
        MatchControlRegistry controlRegistry)
    {
        _db = db;
        _stateFactory = stateFactory;
        _matchOperationLock = matchOperationLock;
        _controlRegistry = controlRegistry;
    }

    public async Task<MatchControlCommandResult> HandleAsync(
        RequestPauseCommand command,
        CancellationToken cancellationToken)
    {
        using var operationLock =
            await _matchOperationLock.AcquireAsync(
                command.MatchId,
                cancellationToken);

        var match = await _db.Matches
            .SingleOrDefaultAsync(
                match =>
                    match.Id == command.MatchId,
                cancellationToken);

        if (match is null)
        {
            return MatchControlCommandResult.Failed(
                MatchControlFailure.MatchNotFound);
        }

        var state =
            _stateFactory.GetState(match.Status);

        if (!state.CanPause(match))
        {
            return MatchControlCommandResult.Failed(
                MatchControlFailure.InvalidState);
        }

        if (!CanRequestControl(
            match,
            command.UserId))
        {
            return MatchControlCommandResult.Failed(
                MatchControlFailure.Forbidden);
        }

        if (match.PauseRequestedByUserId is not null)
        {
            return MatchControlCommandResult.Failed(
                MatchControlFailure
                    .PauseRequestAlreadyExists);
        }

        match.PauseRequestedByUserId =
            command.UserId;

        match.PauseRequestedAt =
            DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(
            cancellationToken);

        return MatchControlCommandResult.Success();
    }

    public async Task<MatchControlCommandResult> HandleAsync(
        PauseMatchCommand command,
        CancellationToken cancellationToken)
    {
        using var operationLock =
            await _matchOperationLock.AcquireAsync(
                command.MatchId,
                cancellationToken);

        var match = await _db.Matches
            .SingleOrDefaultAsync(
                match =>
                    match.Id == command.MatchId,
                cancellationToken);

        if (match is null)
        {
            return MatchControlCommandResult.Failed(
                MatchControlFailure.MatchNotFound);
        }

        if (!CanControl(
            match,
            command.UserId))
        {
            return MatchControlCommandResult.Failed(
                MatchControlFailure.Forbidden);
        }

        var state =
            _stateFactory.GetState(match.Status);

        if (!state.CanPause(match))
        {
            return MatchControlCommandResult.Failed(
                MatchControlFailure.InvalidState);
        }

        if (
            match.TurnDeadlineAt is not null &&
            match.TurnDeadlineAt <=
                DateTimeOffset.UtcNow)
        {
            return MatchControlCommandResult.Failed(
                MatchControlFailure.InvalidState);
        }

        if (match.TurnDeadlineAt is not null)
        {
            var remaining =
                match.TurnDeadlineAt.Value -
                DateTimeOffset.UtcNow;

            match.PausedTurnSecondsRemaining =
                Math.Max(
                    0,
                    (int)Math.Ceiling(
                        remaining.TotalSeconds));
        }

        match.TurnDeadlineAt = null;

        match.Status =
            state.OnPaused();

        match.PauseRequestedByUserId = null;
        match.PauseRequestedAt = null;

        match.ResumeRequestedByUserId = null;
        match.ResumeRequestedAt = null;

        await _db.SaveChangesAsync(
            cancellationToken);

        return MatchControlCommandResult.Success();
    }

    public async Task<MatchControlCommandResult> HandleAsync(
        RejectPauseRequestCommand command,
        CancellationToken cancellationToken)
    {
        using var operationLock =
            await _matchOperationLock.AcquireAsync(
                command.MatchId,
                cancellationToken);

        var match = await _db.Matches
            .SingleOrDefaultAsync(
                match =>
                    match.Id == command.MatchId,
                cancellationToken);

        if (match is null)
        {
            return MatchControlCommandResult.Failed(
                MatchControlFailure.MatchNotFound);
        }

        if (!CanControl(
            match,
            command.UserId))
        {
            return MatchControlCommandResult.Failed(
                MatchControlFailure.Forbidden);
        }

        var state =
            _stateFactory.GetState(match.Status);

        if (!state.CanPause(match))
        {
            return MatchControlCommandResult.Failed(
                MatchControlFailure.InvalidState);
        }

        if (match.PauseRequestedByUserId is null)
        {
            return MatchControlCommandResult.Failed(
                MatchControlFailure
                    .PauseRequestNotFound);
        }

        match.PauseRequestedByUserId = null;
        match.PauseRequestedAt = null;

        await _db.SaveChangesAsync(
            cancellationToken);

        return MatchControlCommandResult.Success();
    }

    public async Task<MatchControlCommandResult> HandleAsync(
        ResumeMatchCommand command,
        CancellationToken cancellationToken)
    {
        using var operationLock =
            await _matchOperationLock.AcquireAsync(
                command.MatchId,
                cancellationToken);

        var match = await _db.Matches
            .SingleOrDefaultAsync(
                match =>
                    match.Id == command.MatchId,
                cancellationToken);

        if (match is null)
        {
            return MatchControlCommandResult.Failed(
                MatchControlFailure.MatchNotFound);
        }

        if (!CanControl(
            match,
            command.UserId))
        {
            return MatchControlCommandResult.Failed(
                MatchControlFailure.Forbidden);
        }

        var state =
            _stateFactory.GetState(match.Status);

        if (!state.CanResume(match))
        {
            return MatchControlCommandResult.Failed(
                MatchControlFailure.InvalidState);
        }

        var remainingSeconds =
            match.PausedTurnSecondsRemaining
            ?? match.TurnDurationSeconds;

        match.Status =
            state.OnResumed();

        match.TurnDeadlineAt =
            DateTimeOffset.UtcNow.AddSeconds(
                remainingSeconds);

        match.PausedTurnSecondsRemaining = null;

        match.ResumeRequestedByUserId = null;
        match.ResumeRequestedAt = null;

        match.PauseRequestedByUserId = null;
        match.PauseRequestedAt = null;

        await _db.SaveChangesAsync(
            cancellationToken);

        return MatchControlCommandResult.Success();
    }

    public async Task<MatchControlCommandResult> HandleAsync(
        RequestResumeCommand command,
        CancellationToken cancellationToken)
    {
        using var operationLock =
            await _matchOperationLock.AcquireAsync(
                command.MatchId,
                cancellationToken);

        var match = await _db.Matches
            .SingleOrDefaultAsync(
                match =>
                    match.Id == command.MatchId,
                cancellationToken);

        if (match is null)
        {
            return MatchControlCommandResult.Failed(
                MatchControlFailure.MatchNotFound);
        }

        var state =
            _stateFactory.GetState(match.Status);

        if (!state.CanResume(match))
        {
            return MatchControlCommandResult.Failed(
                MatchControlFailure.InvalidState);
        }

        if (!CanRequestControl(
            match,
            command.UserId))
        {
            return MatchControlCommandResult.Failed(
                MatchControlFailure.Forbidden);
        }

        if (match.ResumeRequestedByUserId is not null)
        {
            return MatchControlCommandResult.Failed(
                MatchControlFailure
                    .ResumeRequestAlreadyExists);
        }

        match.ResumeRequestedByUserId =
            command.UserId;

        match.ResumeRequestedAt =
            DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(
            cancellationToken);

        return MatchControlCommandResult.Success();
    }

    public async Task<MatchControlCommandResult> HandleAsync(
        RejectResumeRequestCommand command,
        CancellationToken cancellationToken)
    {
        using var operationLock =
            await _matchOperationLock.AcquireAsync(
                command.MatchId,
                cancellationToken);

        var match = await _db.Matches
            .SingleOrDefaultAsync(
                match =>
                    match.Id == command.MatchId,
                cancellationToken);

        if (match is null)
        {
            return MatchControlCommandResult.Failed(
                MatchControlFailure.MatchNotFound);
        }

        if (!CanControl(
            match,
            command.UserId))
        {
            return MatchControlCommandResult.Failed(
                MatchControlFailure.Forbidden);
        }

        if (match.ResumeRequestedByUserId is null)
        {
            return MatchControlCommandResult.Failed(
                MatchControlFailure
                    .ResumeRequestNotFound);
        }

        match.ResumeRequestedByUserId = null;
        match.ResumeRequestedAt = null;

        await _db.SaveChangesAsync(
            cancellationToken);

        return MatchControlCommandResult.Success();
    }

    private bool CanControl(
        GameMatch match,
        Guid userId)
    {
        return
            IsParticipant(match, userId) &&
            _controlRegistry.CanControl(
                match.Id,
                userId);
    }

    private bool CanRequestControl(
        GameMatch match,
        Guid userId)
    {
        if (!IsParticipant(
            match,
            userId))
        {
            return false;
        }

        var controllerUserId =
            _controlRegistry
                .GetControllerUserId(
                    match.Id);

        return
            controllerUserId is not null &&
            controllerUserId != userId;
    }

    private static bool IsParticipant(
        GameMatch match,
        Guid userId)
    {
        return
            match.OwnerUserId == userId ||
            match.OpponentUserId == userId;
    }
}