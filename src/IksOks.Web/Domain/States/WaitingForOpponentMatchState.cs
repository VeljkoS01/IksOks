using IksOks.Web.Domain.Entities;
using IksOks.Web.Domain.Enums;

namespace IksOks.Web.Domain.States;

public sealed class WaitingForOpponentMatchState
    : IMatchState
{
    public MatchStatus Status
        => MatchStatus.WaitingForOpponent;

    public bool CanJoin(GameMatch match)
    {
        return match.OpponentUserId is null;
    }

    public bool CanMakeMove(GameMatch match)
    {
        return false;
    }

    public MatchStatus OnOpponentJoined()
    {
        return MatchStatus.InProgress;
    }

    public MatchStatus OnGameFinished()
    {
        throw new InvalidOperationException(
            "A waiting match cannot be finished.");
    }

    public bool CanPause(GameMatch match)
    {
        return false;
    }

    public bool CanResume(GameMatch match)
    {
        return false;
    }

    public MatchStatus OnPaused()
    {
        throw new InvalidOperationException(
            "A waiting match cannot be paused.");
    }

    public MatchStatus OnResumed()
    {
        throw new InvalidOperationException(
            "A waiting match cannot be resumed.");
    }
}