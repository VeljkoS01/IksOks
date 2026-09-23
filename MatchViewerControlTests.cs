using IksOks.Web.Realtime.Collaboration;

namespace IksOks.Web.Tests.Realtime.Collaboration;

public sealed class MatchViewerRegistryTests
{
    [Fact]
    public void First_viewer_is_editor()
    {
        var registry =
            new MatchViewerRegistry();

        var access =
            registry.Join(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "connection-1");

        Assert.True(
            access.CanEdit);
    }

    [Fact]
    public void Second_viewer_is_read_only()
    {
        var registry =
            new MatchViewerRegistry();

        var matchId =
            Guid.NewGuid();

        registry.Join(
            matchId,
            Guid.NewGuid(),
            "connection-1");

        var second =
            registry.Join(
                matchId,
                Guid.NewGuid(),
                "connection-2");

        Assert.False(
            second.CanEdit);
    }

    [Fact]
    public void Next_viewer_is_promoted_when_editor_leaves()
    {
        var registry =
            new MatchViewerRegistry();

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

        var promotedUserId =
            registry.Leave(
                matchId,
                "connection-1");

        Assert.Equal(
            secondUserId,
            promotedUserId);

        var remaining =
            Assert.Single(
                registry.GetAccess(
                    matchId));

        Assert.True(
            remaining.CanEdit);

        Assert.Equal(
            secondUserId,
            remaining.UserId);
    }

    [Fact]
    public void Leaving_read_only_viewer_does_not_change_editor()
    {
        var registry =
            new MatchViewerRegistry();

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

        var promoted =
            registry.Leave(
                matchId,
                "connection-2");

        Assert.Null(
            promoted);

        var remaining =
            Assert.Single(
                registry.GetAccess(
                    matchId));

        Assert.True(
            remaining.CanEdit);

        Assert.Equal(
            firstUserId,
            remaining.UserId);
    }
}