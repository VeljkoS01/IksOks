using IksOks.Web.Application.Commands;

namespace IksOks.Web.Application.Commands.Matches;

public sealed record MakeMoveCommand(
    Guid MatchId,
    Guid UserId,
    int Row,
    int Column)
    : ICommand<MakeMoveCommandResult>;