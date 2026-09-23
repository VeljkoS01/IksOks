namespace IksOks.Web.Realtime.Collaboration;

public sealed record MatchViewerAccess(
    Guid UserId,
    string ConnectionId,
    bool CanEdit);

public sealed record MatchEditorPromotion(
    Guid MatchId,
    Guid UserId);

public sealed class MatchViewerRegistry
{
    private sealed record Viewer(
        Guid UserId,
        string ConnectionId);

    private readonly object _lock = new();

    private readonly Dictionary<
        Guid,
        List<Viewer>> _viewers = new();

    public MatchViewerAccess Join(
        Guid matchId,
        Guid userId,
        string connectionId)
    {
        lock (_lock)
        {
            if (!_viewers.TryGetValue(
                matchId,
                out var viewers))
            {
                viewers = new List<Viewer>();

                _viewers[matchId] =
                    viewers;
            }

            var existing =
                viewers.FirstOrDefault(
                    viewer =>
                        viewer.ConnectionId ==
                        connectionId);

            if (existing is null)
            {
                viewers.Add(
                    new Viewer(
                        userId,
                        connectionId));
            }

            var editor =
                viewers.First();

            return new MatchViewerAccess(
                userId,
                connectionId,
                editor.ConnectionId ==
                    connectionId);
        }
    }

    public Guid? Leave(
        Guid matchId,
        string connectionId)
    {
        lock (_lock)
        {
            if (!_viewers.TryGetValue(
                matchId,
                out var viewers))
            {
                return null;
            }

            var wasEditor =
                viewers.Count > 0 &&
                viewers[0].ConnectionId ==
                    connectionId;

            viewers.RemoveAll(
                viewer =>
                    viewer.ConnectionId ==
                    connectionId);

            if (viewers.Count == 0)
            {
                _viewers.Remove(matchId);

                return null;
            }

            if (!wasEditor)
            {
                return null;
            }

            return viewers[0].UserId;
        }
    }

    public bool CanEdit(
        Guid matchId,
        string connectionId)
    {
        lock (_lock)
        {
            return
                _viewers.TryGetValue(
                    matchId,
                    out var viewers) &&
                viewers.Count > 0 &&
                viewers[0].ConnectionId ==
                    connectionId;
        }
    }

    public IReadOnlyList<
        MatchViewerAccess> GetAccess(
        Guid matchId)
    {
        lock (_lock)
        {
            if (!_viewers.TryGetValue(
                matchId,
                out var viewers))
            {
                return Array.Empty<
                    MatchViewerAccess>();
            }

            return viewers
                .Select(
                    (viewer, index) =>
                        new MatchViewerAccess(
                            viewer.UserId,
                            viewer.ConnectionId,
                            index == 0))
                .ToList();
        }
    }

    public IReadOnlyList<MatchEditorPromotion>
    RemoveConnection(
        string connectionId)
    {
        lock (_lock)
        {
            var promotions =
                new List<MatchEditorPromotion>();

            var emptyMatches =
                new List<Guid>();

            foreach (var pair in _viewers)
            {
                var matchId =
                    pair.Key;

                var viewers =
                    pair.Value;

                var wasEditor =
                    viewers.Count > 0 &&
                    viewers[0].ConnectionId ==
                        connectionId;

                viewers.RemoveAll(
                    viewer =>
                        viewer.ConnectionId ==
                        connectionId);

                if (viewers.Count == 0)
                {
                    emptyMatches.Add(
                        matchId);

                    continue;
                }

                if (wasEditor)
                {
                    promotions.Add(
                        new MatchEditorPromotion(
                            matchId,
                            viewers[0].UserId));
                }
            }

            foreach (var matchId
                in emptyMatches)
            {
                _viewers.Remove(matchId);
            }

            return promotions;
        }
    }
}