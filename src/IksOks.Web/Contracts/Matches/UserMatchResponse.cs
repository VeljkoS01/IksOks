namespace IksOks.Web.Contracts.Matches;

public sealed record UserMatchResponse(
    Guid Id,
    Guid OwnerUserId,
    string OwnerUserName,
    Guid? OpponentUserId,
    string? OpponentUserName,
    int BoardSize,
    int WinLength,
    string Status,
    Guid? WinnerUserId,
    string? WinnerUserName,
    DateTimeOffset CreatedAt,
    DateTimeOffset? FinishedAt);