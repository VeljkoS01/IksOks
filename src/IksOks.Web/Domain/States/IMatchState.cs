using IksOks.Web.Domain.Entities;
using IksOks.Web.Domain.Enums;

namespace IksOks.Web.Domain.States;

public interface IMatchState
{
    MatchStatus Status { get; }

    bool CanJoin(GameMatch match);

    bool CanMakeMove(GameMatch match);

    MatchStatus OnOpponentJoined();

    MatchStatus OnGameFinished();
}