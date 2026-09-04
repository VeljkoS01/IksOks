namespace IksOks.Web.Messaging.Contracts;

public sealed record MatchFinishedEvent(
    Guid EventId,
    Guid MatchId,
    Guid OwnerUserId,
    Guid OpponentUserId,
    Guid? WinnerUserId,
    bool IsDraw,
    int BoardSize,
    int WinLength,
    DateTimeOffset FinishedAt);