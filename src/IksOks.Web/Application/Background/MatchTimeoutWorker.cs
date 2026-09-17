using System.Text.Json;
using IksOks.Web.Domain.Enums;
using IksOks.Web.Infrastructure.Persistence;
using IksOks.Web.Infrastructure.Persistence.Entities;
using IksOks.Web.Messaging.Contracts;
using IksOks.Web.Realtime;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace IksOks.Web.Application.Background;

public sealed class MatchTimeoutWorker
    : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<MatchHub> _hub;
    private readonly ILogger<MatchTimeoutWorker> _logger;

    public MatchTimeoutWorker(
        IServiceScopeFactory scopeFactory,
        IHubContext<MatchHub> hub,
        ILogger<MatchTimeoutWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _hub = hub;
        _logger = logger;
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

        var expiredMatches =
            await db.Matches
                .Include(match => match.Moves)
                .Where(match =>
                    match.Status ==
                        MatchStatus.InProgress &&
                    match.TurnDeadlineAt != null &&
                    match.TurnDeadlineAt <= now &&
                    match.OpponentUserId != null)
                .ToListAsync(cancellationToken);

        foreach (var match in expiredMatches)
        {
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
                now;

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