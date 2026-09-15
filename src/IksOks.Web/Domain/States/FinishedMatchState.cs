using IksOks.Web.Domain.Entities;
using IksOks.Web.Domain.Enums;

namespace IksOks.Web.Domain.States;

public sealed class FinishedMatchState
    : IMatchState
{
    public MatchStatus Status
        => MatchStatus.Finished;

    public bool CanJoin(GameMatch match)
    {
        return false;
    }

    public bool CanMakeMove(GameMatch match)
    {
        return false;
    }

    public MatchStatus OnOpponentJoined()
    {
        throw new InvalidOperationException(
            "A finished match cannot accept an opponent.");
    }

    public MatchStatus OnGameFinished()
    {
        throw new InvalidOperationException(
            "The match is already finished.");
    }
}