using IksOks.Web.Domain.Entities;
using IksOks.Web.Domain.Enums;

namespace IksOks.Web.Domain.States;

public sealed class InProgressMatchState
    : IMatchState
{
    public MatchStatus Status
        => MatchStatus.InProgress;

    public bool CanJoin(GameMatch match)
    {
        return false;
    }

    public bool CanMakeMove(GameMatch match)
    {
        return match.OpponentUserId is not null;
    }

    public MatchStatus OnOpponentJoined()
    {
        throw new InvalidOperationException(
            "An in-progress match already has an opponent.");
    }

    public MatchStatus OnGameFinished()
    {
        return MatchStatus.Finished;
    }
}