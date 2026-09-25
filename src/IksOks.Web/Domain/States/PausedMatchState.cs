using IksOks.Web.Domain.Entities;
using IksOks.Web.Domain.Enums;

namespace IksOks.Web.Domain.States;

public sealed class PausedMatchState
    : IMatchState
{
    public MatchStatus Status
        => MatchStatus.Paused;

    public bool CanJoin(GameMatch match)
    {
        return false;
    }

    public bool CanMakeMove(GameMatch match)
    {
        return false;
    }

    public bool CanPause(GameMatch match)
    {
        return false;
    }

    public bool CanResume(GameMatch match)
    {
        return true;
    }

    public MatchStatus OnOpponentJoined()
    {
        throw new InvalidOperationException(
            "A paused match already has an opponent.");
    }

    public MatchStatus OnGameFinished()
    {
        throw new InvalidOperationException(
            "A paused match cannot be finished.");
    }

    public MatchStatus OnPaused()
    {
        throw new InvalidOperationException(
            "The match is already paused.");
    }

    public MatchStatus OnResumed()
    {
        return MatchStatus.InProgress;
    }
}