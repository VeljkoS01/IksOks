namespace IksOks.Web.Application.Commands.Matches;

public enum MakeMoveFailure
{
    MatchNotFound,
    MatchNotInProgress,
    Forbidden,
    OutsideBoard,
    Occupied,
    NotYourTurn,
    Conflict
}

public sealed record MakeMoveData(
    Guid Id,
    Guid PlayerUserId,
    int Row,
    int Column,
    int MoveNumber,
    string Symbol,
    DateTimeOffset CreatedAt);

public sealed record MakeMoveCommandResult(
    MakeMoveData? Move,
    MakeMoveFailure? Failure,
    bool MatchFinished)
{
    public bool IsSuccess =>
        Move is not null &&
        Failure is null;

    public static MakeMoveCommandResult Success(
        MakeMoveData move,
        bool matchFinished)
    {
        return new MakeMoveCommandResult(
            move,
            null,
            matchFinished);
    }

    public static MakeMoveCommandResult Failed(
        MakeMoveFailure failure)
    {
        return new MakeMoveCommandResult(
            null,
            failure,
            false);
    }
}