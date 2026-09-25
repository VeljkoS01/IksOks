using IksOks.Web.Realtime.Collaboration;

namespace IksOks.Web.Tests.Realtime.Collaboration;

public sealed class MatchControlRegistryTests
{
    [Fact]
    public void First_participant_gets_control()
    {
        var registry =
            new MatchControlRegistry();

        var matchId =
            Guid.NewGuid();

        var userId =
            Guid.NewGuid();

        var controllerUserId =
            registry.Join(
                matchId,
                userId,
                "connection-1");

        Assert.Equal(
            userId,
            controllerUserId);

        Assert.True(
            registry.CanControl(
                matchId,
                userId));
    }

    [Fact]
    public void Second_participant_does_not_get_control()
    {
        var registry =
            new MatchControlRegistry();

        var matchId =
            Guid.NewGuid();

        var firstUserId =
            Guid.NewGuid();

        var secondUserId =
            Guid.NewGuid();

        registry.Join(
            matchId,
            firstUserId,
            "connection-1");

        registry.Join(
            matchId,
            secondUserId,
            "connection-2");

        Assert.False(
            registry.CanControl(
                matchId,
                secondUserId));

        Assert.Equal(
            firstUserId,
            registry.GetControllerUserId(
                matchId));
    }

    [Fact]
    public void Next_participant_gets_control_when_controller_leaves()
    {
        var registry =
            new MatchControlRegistry();

        var matchId =
            Guid.NewGuid();

        var firstUserId =
            Guid.NewGuid();

        var secondUserId =
            Guid.NewGuid();

        registry.Join(
            matchId,
            firstUserId,
            "connection-1");

        registry.Join(
            matchId,
            secondUserId,
            "connection-2");

        var changed =
            registry.Leave(
                matchId,
                "connection-1");

        Assert.True(changed);

        Assert.Equal(
            secondUserId,
            registry.GetControllerUserId(
                matchId));

        Assert.True(
            registry.CanControl(
                matchId,
                secondUserId));
    }

    [Fact]
    public void Leaving_non_controller_does_not_change_control()
    {
        var registry =
            new MatchControlRegistry();

        var matchId =
            Guid.NewGuid();

        var firstUserId =
            Guid.NewGuid();

        registry.Join(
            matchId,
            firstUserId,
            "connection-1");

        registry.Join(
            matchId,
            Guid.NewGuid(),
            "connection-2");

        var changed =
            registry.Leave(
                matchId,
                "connection-2");

        Assert.False(changed);

        Assert.Equal(
            firstUserId,
            registry.GetControllerUserId(
                matchId));
    }

    [Fact]
    public void Disconnecting_controller_promotes_next_participant()
    {
        var registry =
            new MatchControlRegistry();

        var matchId =
            Guid.NewGuid();

        var secondUserId =
            Guid.NewGuid();

        registry.Join(
            matchId,
            Guid.NewGuid(),
            "connection-1");

        registry.Join(
            matchId,
            secondUserId,
            "connection-2");

        var changedMatches =
            registry.RemoveConnection(
                "connection-1");

        Assert.Contains(
            matchId,
            changedMatches);

        Assert.Equal(
            secondUserId,
            registry.GetControllerUserId(
                matchId));
    }
}