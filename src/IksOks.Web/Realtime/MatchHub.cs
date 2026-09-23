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
    private readonly MatchViewerRegistry _viewerRegistry;

    public MatchHub(IksOksDbContext db, MatchViewerRegistry viewerRegistry)
    {
        _db = db;
        _viewerRegistry = viewerRegistry;
    }

    public static string GroupName(Guid matchId)
    {
        return $"match:{matchId}";
    }

    public async Task JoinMatch(Guid matchId)
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

        var access =
            _viewerRegistry.Join(
                matchId,
                userId,
                Context.ConnectionId);

        await Clients.Caller.SendAsync(
            "MatchAccessChanged",
            new
            {
                matchId,
                canEditSharedNote =
                    access.CanEdit
            },
            Context.ConnectionAborted);
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

    private async Task NotifyAccessChangedAsync(Guid matchId)
    {
        var viewers =
            _viewerRegistry.GetAccess(
                matchId);

        foreach (var viewer in viewers)
        {
            await Clients
                .Client(viewer.ConnectionId)
                .SendAsync(
                    "MatchAccessChanged",
                    new
                    {
                        matchId,
                        canEditSharedNote =
                            viewer.CanEdit
                    },
                    Context.ConnectionAborted);
        }
    }

    public async Task UpdateSharedNote(Guid matchId, string text)
    {
        var userIdValue = Context.User?
            .FindFirst(
                ClaimTypes.NameIdentifier)?
            .Value;

        if (!Guid.TryParse(
            userIdValue,
            out _))
        {
            throw new HubException(
                "Authenticated user was not found.");
        }

        if (!_viewerRegistry.CanEdit(
            matchId,
            Context.ConnectionId))
        {
            throw new HubException(
                "You currently have read-only access.");
        }

        var normalizedText =
            (text ?? string.Empty)
                .Trim();

        if (normalizedText.Length > 1000)
        {
            throw new HubException(
                "Shared note is too long.");
        }

        var match = await _db.Matches
            .SingleOrDefaultAsync(
                match =>
                    match.Id == matchId,
                Context.ConnectionAborted);

        if (match is null)
        {
            throw new HubException(
                "Match was not found.");
        }

        match.SharedNote =
            normalizedText;

        await _db.SaveChangesAsync(
            Context.ConnectionAborted);

        await Clients
            .Group(GroupName(matchId))
            .SendAsync(
                "SharedNoteUpdated",
                new
                {
                    matchId,
                    text = match.SharedNote
                },
                Context.ConnectionAborted);
    }

    public async Task LeaveMatch(Guid matchId)
    {
        var promotedUserId =
            _viewerRegistry.Leave(
                matchId,
                Context.ConnectionId);

        await Groups.RemoveFromGroupAsync(
            Context.ConnectionId,
            GroupName(matchId),
            Context.ConnectionAborted);

        if (promotedUserId is not null)
        {
            await NotifyAccessChangedAsync(
                matchId);
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var promotions =
            _viewerRegistry.RemoveConnection(
                Context.ConnectionId);

        foreach (var promotion
            in promotions)
        {
            await NotifyAccessChangedAsync(
                promotion.MatchId);
        }

        await base.OnDisconnectedAsync(
            exception);
    }
}