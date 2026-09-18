namespace IksOks.Web.Contracts.Matches;

public sealed record LiveMatchResponse(
    Guid Id,
    Guid OwnerUserId,
    string OwnerUserName,
    Guid OpponentUserId,
    string OpponentUserName,
    string Mode,
    int BoardSize,
    int WinLength,
    string Status,
    DateTimeOffset CreatedAt);