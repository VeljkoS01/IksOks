namespace IksOks.Web.Contracts.Matches;

public sealed record CreateMatchRequest(
    string Mode,
    string Visibility,
    int BoardSize,
    int WinLength);