namespace IksOks.Web.Contracts.Users;

public sealed record UserProfileResponse(
    Guid UserId,
    string UserName,
    DateTimeOffset MemberSince,
    int TokenBalance,
    int MatchesPlayed,
    int Wins,
    int Draws,
    int Losses,
    int Points,
    double WinRate);

