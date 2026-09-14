using IksOks.Web.Domain.Entities;

namespace IksOks.Web.Domain.Strategies;

public interface IGameRulesStrategy
{
    bool IsValidConfiguration(
        int boardSize,
        int winLength);

    bool IsWinningMove(
        IEnumerable<MatchMove> moves,
        MatchMove lastMove,
        int winLength);
}