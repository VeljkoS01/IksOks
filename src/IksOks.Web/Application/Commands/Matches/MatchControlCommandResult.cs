namespace IksOks.Web.Application.Commands.Matches;

public enum MatchControlFailure
{
    MatchNotFound,
    Forbidden,
    InvalidState,
    PauseRequestAlreadyExists,
    PauseRequestNotFound
}

public sealed record MatchControlCommandResult(
    bool IsSuccess,
    MatchControlFailure? Failure)
{
    public static MatchControlCommandResult Success()
    {
        return new MatchControlCommandResult(
            true,
            null);
    }

    public static MatchControlCommandResult Failed(
        MatchControlFailure failure)
    {
        return new MatchControlCommandResult(
            false,
            failure);
    }
}