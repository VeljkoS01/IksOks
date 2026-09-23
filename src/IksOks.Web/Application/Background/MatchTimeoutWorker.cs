using System.Text.Json;
using IksOks.Web.Domain.Enums;
using IksOks.Web.Infrastructure.Persistence;
using IksOks.Web.Infrastructure.Persistence.Entities;
using IksOks.Web.Messaging.Contracts;
using IksOks.Web.Realtime;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using IksOks.Web.Application.Concurrency;

namespace IksOks.Web.Application.Background;

public sealed class MatchTimeoutWorker
    : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<MatchHub> _hub;
    private readonly ILogger<MatchTimeoutWorker> _logger;
    private readonly MatchOperationLock _matchOperationLock;

    public MatchTimeoutWorker(
        IServiceScopeFactory scopeFactory,
        IHubContext<MatchHub> hub,
        ILogger<MatchTimeoutWorker> logger,
        MatchOperationLock matchOperationLock)
    {
        _scopeFactory = scopeFactory;
        _hub = hub;
        _logger = logger;
        _matchOperationLock = matchOperationLock;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessTimeoutsAsync(
                    stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Could not process match timeouts.");
            }

            await Task.Delay(
                TimeSpan.FromSeconds(1),
                stoppingToken);
        }
    }

    private async Task ProcessTimeoutsAsync(
        CancellationToken cancellationToken)
    {
        using var scope =
            _scopeFactory.CreateScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<IksOksDbContext>();

        var now =
    DateTimeOffset.UtcNow;

        var expiredMatchIds =
            await db.Matches
                .AsNoTracking()
                .Where(match =>
                    match.Status ==
                        MatchStatus.InProgress &&
                    match.TurnDeadlineAt != null &&
                    match.TurnDeadlineAt <= now &&
                    match.OpponentUserId != null)
                .Select(match => match.Id)
                .ToListAsync(
                    cancellationToken);

        foreach (var matchId in expiredMatchIds)
        {

            using var operationLock =
                await _matchOperationLock.AcquireAsync(
                matchId,
                cancellationToken);

            var checkTime =
                DateTimeOffset.UtcNow;

            var match = await db.Matches
                .Include(match => match.Moves)
                .SingleOrDefaultAsync(
                    match =>
                        match.Id == matchId,
                    cancellationToken);

            if (
                match is null ||
                match.Status !=
                    MatchStatus.InProgress ||
                match.TurnDeadlineAt is null ||
                match.TurnDeadlineAt > checkTime ||
                match.OpponentUserId is null)
            {
                continue;
            }

            var opponentId =
                match.OpponentUserId!.Value;

            var currentTurnUserId =
                match.Moves.Count % 2 == 0
                    ? match.OwnerUserId
                    : opponentId;

            var winnerUserId =
                currentTurnUserId ==
                    match.OwnerUserId
                    ? opponentId
                    : match.OwnerUserId;

            match.Status =
                MatchStatus.Finished;

            match.WinnerUserId =
                winnerUserId;

            match.FinishedAt = 
                checkTime;

            match.TurnDeadlineAt = null;
            match.PausedTurnSecondsRemaining = null;

            var eventId =
                Guid.NewGuid();

            var finishedEvent =
                new MatchFinishedEvent(
                    eventId,
                    match.Id,
                    match.OwnerUserId,
                    opponentId,
                    match.WinnerUserId,
                    false,
                    match.BoardSize,
                    match.WinLength,
                    match.FinishedAt.Value);

            db.OutboxMessages.Add(
                new OutboxMessage
                {
                    Id = eventId,
                    RoutingKey =
                        "match.finished",
                    Payload =
                        JsonSerializer.Serialize(
                            finishedEvent),
                    OccurredAt =
                        match.FinishedAt.Value
                });

            await db.SaveChangesAsync(
                cancellationToken);

            await _hub.Clients
                .Group(
                    MatchHub.GroupName(
                        match.Id))
                .SendAsync(
                    "MatchUpdated",
                    match.Id,
                    cancellationToken);

            await _hub.Clients.All
                .SendAsync(
                    "LobbyUpdated",
                    cancellationToken);
        }
    }
}