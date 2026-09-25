namespace IksOks.Web.Contracts.Matches;

public sealed record MatchResponse(
    Guid Id,
    Guid OwnerUserId,
    string OwnerUserName,
    Guid? OpponentUserId,
    string? OpponentUserName,
    string Mode,
    string Visibility,
    string? JoinCode,
    int BoardSize,
    int WinLength,
    string Status,
    DateTimeOffset CreatedAt);