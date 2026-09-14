namespace IksOks.Web.Contracts.Matches;

public sealed record CreateMatchRequest(
    string Mode,
    int BoardSize,
    int WinLength);