namespace IksOks.Web.Realtime.Collaboration;

public sealed class MatchControlRegistry
{
    private sealed record Participant(
        Guid UserId,
        string ConnectionId);

    private readonly object _lock = new();

    private readonly Dictionary<
        Guid,
        List<Participant>> _participants = new();

    public Guid Join(
        Guid matchId,
        Guid userId,
        string connectionId)
    {
        lock (_lock)
        {
            if (!_participants.TryGetValue(
                matchId,
                out var participants))
            {
                participants =
                    new List<Participant>();

                _participants[matchId] =
                    participants;
            }

            var alreadyJoined =
                participants.Any(
                    participant =>
                        participant.ConnectionId ==
                        connectionId);

            if (!alreadyJoined)
            {
                participants.Add(
                    new Participant(
                        userId,
                        connectionId));
            }

            return participants[0].UserId;
        }
    }

    public Guid? GetControllerUserId(
        Guid matchId)
    {
        lock (_lock)
        {
            if (!_participants.TryGetValue(
                matchId,
                out var participants) ||
                participants.Count == 0)
            {
                return null;
            }

            return participants[0].UserId;
        }
    }

    public bool CanControl(
        Guid matchId,
        Guid userId)
    {
        lock (_lock)
        {
            return
                _participants.TryGetValue(
                    matchId,
                    out var participants) &&
                participants.Count > 0 &&
                participants[0].UserId ==
                    userId;
        }
    }

    public bool Leave(
        Guid matchId,
        string connectionId)
    {
        lock (_lock)
        {
            if (!_participants.TryGetValue(
                matchId,
                out var participants))
            {
                return false;
            }

            var wasController =
                participants.Count > 0 &&
                participants[0].ConnectionId ==
                    connectionId;

            var removed =
                participants.RemoveAll(
                    participant =>
                        participant.ConnectionId ==
                        connectionId);

            if (participants.Count == 0)
            {
                _participants.Remove(
                    matchId);
            }

            return removed > 0 &&
                   wasController;
        }
    }

    public IReadOnlyList<Guid>
        RemoveConnection(
            string connectionId)
    {
        lock (_lock)
        {
            var changedMatches =
                new List<Guid>();

            var emptyMatches =
                new List<Guid>();

            foreach (var pair
                in _participants)
            {
                var matchId =
                    pair.Key;

                var participants =
                    pair.Value;

                var wasController =
                    participants.Count > 0 &&
                    participants[0]
                        .ConnectionId ==
                    connectionId;

                var removed =
                    participants.RemoveAll(
                        participant =>
                            participant.ConnectionId ==
                            connectionId);

                if (removed == 0)
                {
                    continue;
                }

                if (wasController)
                {
                    changedMatches.Add(
                        matchId);
                }

                if (participants.Count == 0)
                {
                    emptyMatches.Add(
                        matchId);
                }
            }

            foreach (var matchId
                in emptyMatches)
            {
                _participants.Remove(
                    matchId);
            }

            return changedMatches;
        }
    }
}