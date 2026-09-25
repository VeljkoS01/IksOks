namespace IksOks.Web.Contracts.Users;

public sealed record LeaderboardEntryResponse(
    int Rank,
    Guid UserId,
    string UserName,
    int MatchesPlayed,
    int Wins,
    int Draws,
    int Losses,
    int Points,
    double WinRate);