using System.Security.Claims;
using IksOks.Web.Contracts.Matches;
using IksOks.Web.Domain.Enums;
using IksOks.Web.Infrastructure.Persistence;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using IksOks.Web.Domain.Store;
using IksOks.Web.Realtime.Collaboration;

namespace IksOks.Web.Realtime;

public sealed class MatchHub : Hub
{
    private static readonly HashSet<string> BasicEmojis = new(StringComparer.Ordinal)
    {
        "😀",
        "😂",
        "😎",
        "🔥",
        "👏",
        "🤔",
        "😢",
        "😡"
    };
    private readonly IksOksDbContext _db;
    private readonly MatchControlRegistry _controlRegistry;

    public MatchHub(IksOksDbContext db, MatchControlRegistry controlRegistry)
    {
        _db = db;
        _controlRegistry = controlRegistry;
    }

    public static string GroupName(Guid matchId)
    {
        return $"match:{matchId}";
    }

    public async Task<Guid?> JoinMatch(Guid matchId)
    {
        var userIdValue = Context.User?
            .FindFirst(ClaimTypes.NameIdentifier)?
            .Value;

        if (!Guid.TryParse(userIdValue, out var userId))
        {
            throw new HubException(
                "Authenticated user was not found.");
        }

        var match = await _db.Matches
            .AsNoTracking()
            .SingleOrDefaultAsync(
                match => match.Id == matchId,
                Context.ConnectionAborted);

        if (match is null)
        {
            throw new HubException(
                "Match was not found.");
        }

        var isParticipant =
            match.OwnerUserId == userId ||
            match.OpponentUserId == userId;

        var canSpectate =
            match.Visibility ==
                MatchVisibility.Public &&
            (
                match.Status ==
                    MatchStatus.InProgress ||
                match.Status ==
                    MatchStatus.Paused ||
                match.Status ==
                    MatchStatus.Finished
            );

        if (!isParticipant && !canSpectate)
        {
            throw new HubException(
                "You do not have access to this match.");
        }

        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            GroupName(matchId),
            Context.ConnectionAborted);

        if (isParticipant)
        {
            _controlRegistry.Join(
                matchId,
                userId,
                Context.ConnectionId);
        }

        return _controlRegistry
            .GetControllerUserId(
                matchId);
    }

    public async Task SendEmoji(Guid matchId, string emoji)
    {
        var userIdValue = Context.User?
            .FindFirst(ClaimTypes.NameIdentifier)?
            .Value;

        if (!Guid.TryParse(
            userIdValue,
            out var userId))
        {
            throw new HubException(
                "Authenticated user was not found.");
        }

        if (!BasicEmojis.Contains(emoji))
        {
            var premiumEmoji =
                StoreCatalog.FindEmoji(emoji);

            if (premiumEmoji is null)
            {
                throw new HubException(
                    "Emoji is not allowed.");
            }

            var ownsEmoji =
                await _db.UserPurchases
                    .AsNoTracking()
                    .AnyAsync(
                        purchase =>
                            purchase.UserId == userId &&
                            purchase.ItemKey ==
                            premiumEmoji.Key,
                        Context.ConnectionAborted);

            if (!ownsEmoji)
            {
                throw new HubException(
                    "You do not own this emoji.");
            }
        }

        var match = await _db.Matches
            .AsNoTracking()
            .SingleOrDefaultAsync(
                match => match.Id == matchId,
                Context.ConnectionAborted);

        if (match is null)
        {
            throw new HubException(
                "Match was not found.");
        }

        var isParticipant =
            match.OwnerUserId == userId ||
            match.OpponentUserId == userId;

        if (!isParticipant)
        {
            throw new HubException(
                "Only match participants can send emojis.");
        }

        var canChat =
            match.Status == MatchStatus.InProgress ||
            match.Status == MatchStatus.Paused;

        if (!canChat)
        {
            throw new HubException(
                "Emoji chat is not available in the current match state.");
        }

        var userName =
            Context.User?
                .FindFirst(ClaimTypes.Name)?
                .Value
            ?? "Igrač";

        var message =
            new EmojiChatMessage(
                match.Id,
                userId,
                userName,
                emoji,
                DateTimeOffset.UtcNow);

        await Clients
            .Group(GroupName(match.Id))
            .SendAsync(
                "EmojiReceived",
                message,
                Context.ConnectionAborted);
    }

    private async Task ClearPendingControlRequestsAsync(
    Guid matchId)
    {
        var match = await _db.Matches
            .SingleOrDefaultAsync(
                match =>
                    match.Id == matchId,
                CancellationToken.None);

        if (match is null)
        {
            return;
        }

        var hasPendingRequest =
            match.PauseRequestedByUserId is not null ||
            match.ResumeRequestedByUserId is not null;

        if (!hasPendingRequest)
        {
            return;
        }

        match.PauseRequestedByUserId = null;
        match.PauseRequestedAt = null;

        match.ResumeRequestedByUserId = null;
        match.ResumeRequestedAt = null;

        await _db.SaveChangesAsync(
            CancellationToken.None);
    }

    private async Task NotifyControlChangedAsync(
        Guid matchId)
    {
        var controllerUserId =
            _controlRegistry
                .GetControllerUserId(
                    matchId);

        await Clients
            .Group(GroupName(matchId))
            .SendAsync(
                "MatchControlChanged",
                new
                {
                    matchId,
                    controllerUserId
                },
                CancellationToken.None);
    }

    private async Task HandleControlTransferAsync(
        Guid matchId)
    {
        await ClearPendingControlRequestsAsync(
            matchId);

        await NotifyControlChangedAsync(
            matchId);
    }

    public async Task LeaveMatch(
    Guid matchId)
    {
        var controlChanged =
            _controlRegistry.Leave(
                matchId,
                Context.ConnectionId);

        await Groups.RemoveFromGroupAsync(
            Context.ConnectionId,
            GroupName(matchId),
            Context.ConnectionAborted);

        if (controlChanged)
        {
            await HandleControlTransferAsync(
                matchId);
        }
    }

    public override async Task OnDisconnectedAsync(
    Exception? exception)
    {
        var changedMatches =
            _controlRegistry
                .RemoveConnection(
                    Context.ConnectionId);

        foreach (var matchId
            in changedMatches)
        {
            await HandleControlTransferAsync(
                matchId);
        }

        await base.OnDisconnectedAsync(
            exception);
    }
}