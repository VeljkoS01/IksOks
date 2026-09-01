using IksOks.Web.Domain.Enums;

namespace IksOks.Web.Domain.Entities;

public sealed class GameMatch
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid OwnerUserId { get; set; }

    public AppUser OwnerUser { get; set; } = null!;

    public Guid? OpponentUserId { get; set; }

    public AppUser? OpponentUser { get; set; }

    public int BoardSize { get; set; }

    public int WinLength { get; set; }

    public MatchStatus Status { get; set; }
        = MatchStatus.WaitingForOpponent;

    public DateTimeOffset CreatedAt { get; set; }
        = DateTimeOffset.UtcNow;

    public Guid? WinnerUserId { get; set; }

    public AppUser? WinnerUser { get; set; }

    public DateTimeOffset? FinishedAt { get; set; }

    public ICollection<MatchMove> Moves { get; set; }
        = new List<MatchMove>();
}