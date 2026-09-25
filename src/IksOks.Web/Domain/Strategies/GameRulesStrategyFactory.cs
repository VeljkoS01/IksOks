using IksOks.Web.Domain.Enums;

namespace IksOks.Web.Domain.Strategies;

public sealed class GameRulesStrategyFactory
{
    private readonly ClassicGameRulesStrategy
        _classicStrategy;

    private readonly ConnectKGameRulesStrategy
        _connectKStrategy;

    public GameRulesStrategyFactory(
        ClassicGameRulesStrategy classicStrategy,
        ConnectKGameRulesStrategy connectKStrategy)
    {
        _classicStrategy = classicStrategy;
        _connectKStrategy = connectKStrategy;
    }

    public IGameRulesStrategy GetStrategy(
        MatchMode mode)
    {
        return mode switch
        {
            MatchMode.Classic =>
                _classicStrategy,

            MatchMode.ConnectK =>
                _connectKStrategy,

            _ => throw new ArgumentOutOfRangeException(
                nameof(mode),
                mode,
                "Unsupported match mode.")
        };
    }
}