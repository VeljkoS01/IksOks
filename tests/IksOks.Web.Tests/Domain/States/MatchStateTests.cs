using IksOks.Web.Domain.Entities;
using IksOks.Web.Domain.Enums;
using IksOks.Web.Domain.States;

namespace IksOks.Web.Tests.Domain.States;

public sealed class MatchStateTests
{
    [Fact]
    public void Waiting_match_can_be_joined()
    {
        var state =
            new WaitingForOpponentMatchState();

        var match =
            new GameMatch
            {
                OpponentUserId = null
            };

        Assert.True(
            state.CanJoin(match));

        Assert.False(
            state.CanMakeMove(match));

        Assert.Equal(
            MatchStatus.InProgress,
            state.OnOpponentJoined());
    }

    [Fact]
    public void InProgress_match_can_be_played_and_paused()
    {
        var state =
            new InProgressMatchState();

        var match =
            new GameMatch
            {
                OpponentUserId =
                    Guid.NewGuid()
            };

        Assert.True(
            state.CanMakeMove(match));

        Assert.True(
            state.CanPause(match));

        Assert.Equal(
            MatchStatus.Paused,
            state.OnPaused());

        Assert.Equal(
            MatchStatus.Finished,
            state.OnGameFinished());
    }

    [Fact]
    public void Paused_match_cannot_be_played()
    {
        var state =
            new PausedMatchState();

        var match =
            new GameMatch
            {
                OpponentUserId =
                    Guid.NewGuid()
            };

        Assert.False(
            state.CanMakeMove(match));

        Assert.True(
            state.CanResume(match));

        Assert.Equal(
            MatchStatus.InProgress,
            state.OnResumed());
    }

    [Fact]
    public void Finished_match_cannot_be_changed()
    {
        var state =
            new FinishedMatchState();

        var match =
            new GameMatch();

        Assert.False(
            state.CanJoin(match));

        Assert.False(
            state.CanMakeMove(match));

        Assert.False(
            state.CanPause(match));

        Assert.False(
            state.CanResume(match));
    }
}