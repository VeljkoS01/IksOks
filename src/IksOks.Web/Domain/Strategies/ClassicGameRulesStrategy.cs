using IksOks.Web.Domain.Entities;

namespace IksOks.Web.Domain.Strategies;

public sealed class ClassicGameRulesStrategy
    : IGameRulesStrategy
{
    public bool IsValidConfiguration(
        int boardSize,
        int winLength)
    {
        return boardSize == 3 &&
               winLength == 3;
    }

    public bool IsWinningMove(
        IEnumerable<MatchMove> moves,
        MatchMove lastMove,
        int winLength)
    {
        return ConsecutiveSymbolsDetector
            .HasWinningSequence(
                moves,
                lastMove,
                3);
    }
}