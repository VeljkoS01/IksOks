using IksOks.Web.Domain.Entities;

namespace IksOks.Web.Domain.Strategies;

internal static class ConsecutiveSymbolsDetector
{
    public static bool HasWinningSequence(
        IEnumerable<MatchMove> moves,
        MatchMove lastMove,
        int requiredLength)
    {
        var positions = moves
            .Where(move =>
                move.Symbol == lastMove.Symbol)
            .Select(move =>
                (move.Row, move.Column))
            .ToHashSet();

        var directions = new[]
        {
            (Row: 1, Column: 0),
            (Row: 0, Column: 1),
            (Row: 1, Column: 1),
            (Row: 1, Column: -1)
        };

        foreach (var direction in directions)
        {
            var count = 1;

            count += CountDirection(
                positions,
                lastMove.Row,
                lastMove.Column,
                direction.Row,
                direction.Column);

            count += CountDirection(
                positions,
                lastMove.Row,
                lastMove.Column,
                -direction.Row,
                -direction.Column);

            if (count >= requiredLength)
            {
                return true;
            }
        }

        return false;
    }

    private static int CountDirection(
        HashSet<(int Row, int Column)> positions,
        int row,
        int column,
        int rowStep,
        int columnStep)
    {
        var count = 0;

        var currentRow = row + rowStep;
        var currentColumn = column + columnStep;

        while (positions.Contains(
            (currentRow, currentColumn)))
        {
            count++;

            currentRow += rowStep;
            currentColumn += columnStep;
        }

        return count;
    }
}