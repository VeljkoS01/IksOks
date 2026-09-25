using IksOks.Web.Domain.Entities;
using IksOks.Web.Domain.Strategies;

namespace IksOks.Web.Tests.Domain.Strategies;

public sealed class GameRulesStrategyTests
{
    [Fact]
    public void Classic_accepts_only_three_by_three()
    {
        var strategy =
            new ClassicGameRulesStrategy();

        Assert.True(
            strategy.IsValidConfiguration(
                3,
                3));

        Assert.False(
            strategy.IsValidConfiguration(
                5,
                4));
    }

    [Fact]
    public void Classic_detects_horizontal_win()
    {
        var strategy =
            new ClassicGameRulesStrategy();

        var moves =
            new List<MatchMove>
            {
                new()
                {
                    Row = 0,
                    Column = 0,
                    Symbol = "X"
                },
                new()
                {
                    Row = 0,
                    Column = 1,
                    Symbol = "X"
                },
                new()
                {
                    Row = 0,
                    Column = 2,
                    Symbol = "X"
                }
            };

        var lastMove =
            moves[^1];

        var won =
            strategy.IsWinningMove(
                moves,
                lastMove,
                3);

        Assert.True(won);
    }

    [Fact]
    public void Classic_does_not_count_other_symbol()
    {
        var strategy =
            new ClassicGameRulesStrategy();

        var moves =
            new List<MatchMove>
            {
                new()
                {
                    Row = 0,
                    Column = 0,
                    Symbol = "X"
                },
                new()
                {
                    Row = 0,
                    Column = 1,
                    Symbol = "O"
                },
                new()
                {
                    Row = 0,
                    Column = 2,
                    Symbol = "X"
                }
            };

        var won =
            strategy.IsWinningMove(
                moves,
                moves[^1],
                3);

        Assert.False(won);
    }

    [Fact]
    public void ConnectK_accepts_custom_configuration()
    {
        var strategy =
            new ConnectKGameRulesStrategy();

        Assert.True(
            strategy.IsValidConfiguration(
                5,
                4));

        Assert.False(
            strategy.IsValidConfiguration(
                5,
                6));
    }

    [Fact]
    public void ConnectK_detects_diagonal_win()
    {
        var strategy =
            new ConnectKGameRulesStrategy();

        var moves =
            new List<MatchMove>
            {
                new()
                {
                    Row = 0,
                    Column = 0,
                    Symbol = "O"
                },
                new()
                {
                    Row = 1,
                    Column = 1,
                    Symbol = "O"
                },
                new()
                {
                    Row = 2,
                    Column = 2,
                    Symbol = "O"
                },
                new()
                {
                    Row = 3,
                    Column = 3,
                    Symbol = "O"
                }
            };

        var won =
            strategy.IsWinningMove(
                moves,
                moves[^1],
                4);

        Assert.True(won);
    }
}