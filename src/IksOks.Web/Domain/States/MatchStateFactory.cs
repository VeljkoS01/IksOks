using IksOks.Web.Domain.Enums;

namespace IksOks.Web.Domain.States;

public sealed class MatchStateFactory
{
    private readonly IReadOnlyDictionary<
        MatchStatus,
        IMatchState> _states;

    public MatchStateFactory(
        IEnumerable<IMatchState> states)
    {
        _states = states.ToDictionary(
            state => state.Status);
    }

    public IMatchState GetState(
        MatchStatus status)
    {
        if (_states.TryGetValue(
            status,
            out var state))
        {
            return state;
        }

        throw new ArgumentOutOfRangeException(
            nameof(status),
            status,
            "Unsupported match status.");
    }
}