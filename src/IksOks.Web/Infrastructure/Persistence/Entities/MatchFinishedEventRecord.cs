namespace IksOks.Web.Infrastructure.Persistence.Entities;

public sealed class MatchFinishedEventRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid EventId { get; set; }

    public Guid MatchId { get; set; }

    public Guid? WinnerUserId { get; set; }

    public bool IsDraw { get; set; }

    public DateTimeOffset FinishedAt { get; set; }

    public DateTimeOffset ProcessedAt { get; set; }
        = DateTimeOffset.UtcNow;
}