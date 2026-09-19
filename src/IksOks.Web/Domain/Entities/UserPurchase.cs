namespace IksOks.Web.Domain.Entities;

public sealed class UserPurchase
{
    public Guid Id { get; set; } =
        Guid.NewGuid();

    public Guid UserId { get; set; }

    public AppUser User { get; set; } =
        null!;

    public string ItemKey { get; set; } =
        string.Empty;

    public DateTimeOffset PurchasedAt { get; set; } =
        DateTimeOffset.UtcNow;
}