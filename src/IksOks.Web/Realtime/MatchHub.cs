using System.Security.Claims;
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

        var canJoin = await _db.Matches
            .AsNoTracking()
            .AnyAsync(
                match =>
                    match.Id == matchId &&
                    (
                        match.OwnerUserId == userId ||
                        match.OpponentUserId == userId
                    ),
                Context.ConnectionAborted);

        if (!canJoin)
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