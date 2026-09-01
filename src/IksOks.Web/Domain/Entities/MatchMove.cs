namespace IksOks.Web.Domain.Entities;

public sealed class MatchMove
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid MatchId { get; set; }

    public GameMatch Match { get; set; } = null!;

    public Guid PlayerUserId { get; set; }

    public AppUser PlayerUser { get; set; } = null!;

    public int Row { get; set; }

    public int Column { get; set; }

    public int MoveNumber { get; set; }

    public string Symbol { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }
        = DateTimeOffset.UtcNow;
}