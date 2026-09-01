namespace IksOks.Web.Contracts.Matches;

public sealed record MatchDetailsResponse(
    Guid Id,
    Guid OwnerUserId,
    string OwnerUserName,
    Guid? OpponentUserId,
    string? OpponentUserName,
    int BoardSize,
    int WinLength,
    string Status,
    Guid? CurrentTurnUserId,
    Guid? WinnerUserId,
    string? WinnerUserName,
    DateTimeOffset CreatedAt,
    DateTimeOffset? FinishedAt,
    IReadOnlyList<MoveResponse> Moves);