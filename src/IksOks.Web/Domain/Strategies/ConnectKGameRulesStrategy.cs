using IksOks.Web.Domain.Entities;

namespace IksOks.Web.Domain.Strategies;

public sealed class ConnectKGameRulesStrategy
    : IGameRulesStrategy
{
    public bool IsValidConfiguration(
        int boardSize,
        int winLength)
    {
        return boardSize >= 3 &&
               boardSize <= 10 &&
               winLength >= 3 &&
               winLength <= boardSize;
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
                winLength);
    }
}