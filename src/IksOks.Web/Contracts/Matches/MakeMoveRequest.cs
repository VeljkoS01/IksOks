namespace IksOks.Web.Contracts.Matches;

public sealed record MakeMoveRequest(
    int Row,
    int Column);