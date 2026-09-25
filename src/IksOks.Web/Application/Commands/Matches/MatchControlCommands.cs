using IksOks.Web.Application.Commands;

namespace IksOks.Web.Application.Commands.Matches;

public sealed record RequestPauseCommand(
    Guid MatchId,
    Guid UserId)
    : ICommand<MatchControlCommandResult>;

public sealed record PauseMatchCommand(
    Guid MatchId,
    Guid UserId)
    : ICommand<MatchControlCommandResult>;

public sealed record RejectPauseRequestCommand(
    Guid MatchId,
    Guid UserId)
    : ICommand<MatchControlCommandResult>;

public sealed record ResumeMatchCommand(
    Guid MatchId,
    Guid UserId)
    : ICommand<MatchControlCommandResult>;

public sealed record RequestResumeCommand(
    Guid MatchId,
    Guid UserId)
    : ICommand<MatchControlCommandResult>;

public sealed record RejectResumeRequestCommand(
    Guid MatchId,
    Guid UserId)
    : ICommand<MatchControlCommandResult>;