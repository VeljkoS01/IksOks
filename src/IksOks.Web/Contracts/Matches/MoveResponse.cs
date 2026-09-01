namespace IksOks.Web.Contracts.Matches;

public sealed record MoveResponse(
    Guid Id,
    Guid PlayerUserId,
    int Row,
    int Column,
    int MoveNumber,
    string Symbol,
    DateTimeOffset CreatedAt);