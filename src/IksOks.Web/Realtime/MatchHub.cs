using System.Security.Claims;
using IksOks.Web.Domain.Enums;
using IksOks.Web.Infrastructure.Persistence;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace IksOks.Web.Realtime;

public sealed class MatchHub : Hub
{
    private readonly IksOksDbContext _db;

    public MatchHub(IksOksDbContext db)
    {
        _db = db;
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
            match.Status == MatchStatus.InProgress ||
            match.Status == MatchStatus.Paused ||
            match.Status == MatchStatus.Finished;

        if (!isParticipant && !canSpectate)
        {
            throw new HubException(
                "You do not have access to this match.");
        }

        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            GroupName(matchId),
            Context.ConnectionAborted);
    }

    public async Task LeaveMatch(Guid matchId)
    {
        await Groups.RemoveFromGroupAsync(
            Context.ConnectionId,
            GroupName(matchId),
            Context.ConnectionAborted);
    }
}