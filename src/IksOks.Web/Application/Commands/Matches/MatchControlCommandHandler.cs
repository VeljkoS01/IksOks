using IksOks.Web.Domain.States;
using IksOks.Web.Infrastructure.Persistence;
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

    public MatchControlCommandHandler(
        IksOksDbContext db,
        MatchStateFactory stateFactory)
    {
        _db = db;
        _stateFactory = stateFactory;
    }

    public async Task<MatchControlCommandResult> HandleAsync(
        RequestPauseCommand command,
        CancellationToken cancellationToken)
    {
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

        if (match.OpponentUserId != command.UserId)
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

        if (match.OwnerUserId != command.UserId)
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

        match.Status =
            state.OnPaused();

        match.PauseRequestedByUserId = null;
        match.PauseRequestedAt = null;

        await _db.SaveChangesAsync(
            cancellationToken);

        return MatchControlCommandResult.Success();
    }

    public async Task<MatchControlCommandResult> HandleAsync(
        RejectPauseRequestCommand command,
        CancellationToken cancellationToken)
    {
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

        if (match.OwnerUserId != command.UserId)
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

        if (match.OwnerUserId != command.UserId)
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

        match.Status =
            state.OnResumed();

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
        var match = await _db.Matches
            .SingleOrDefaultAsync(
                match => match.Id == command.MatchId,
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

        if (match.OpponentUserId != command.UserId)
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
        var match = await _db.Matches
            .SingleOrDefaultAsync(
                match => match.Id == command.MatchId,
                cancellationToken);

        if (match is null)
        {
            return MatchControlCommandResult.Failed(
                MatchControlFailure.MatchNotFound);
        }

        if (match.OwnerUserId != command.UserId)
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
}