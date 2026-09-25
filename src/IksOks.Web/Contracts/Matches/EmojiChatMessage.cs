namespace IksOks.Web.Contracts.Matches;

public sealed record EmojiChatMessage(
    Guid MatchId,
    Guid UserId,
    string UserName,
    string Emoji,
    DateTimeOffset SentAt);