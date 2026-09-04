using IksOks.Web.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using IksOks.Web.Infrastructure.Persistence.Entities;

namespace IksOks.Web.Infrastructure.Persistence;

public sealed class IksOksDbContext : DbContext
{
    public IksOksDbContext(DbContextOptions<IksOksDbContext> options)
        : base(options)
    {
    }

    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<GameMatch> Matches => Set<GameMatch>();
    public DbSet<MatchMove> MatchMoves => Set<MatchMove>();
    public DbSet<MatchFinishedEventRecord> MatchFinishedEvents => Set<MatchFinishedEventRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var user = modelBuilder.Entity<AppUser>();

        user.ToTable("Users");

        user.HasKey(x => x.Id);

        user.Property(x => x.UserName)
            .HasMaxLength(32)
            .IsRequired();

        user.Property(x => x.NormalizedUserName)
            .HasMaxLength(32)
            .IsRequired();

        user.HasIndex(x => x.NormalizedUserName)
            .IsUnique();

        user.Property(x => x.PasswordHash)
            .HasMaxLength(512)
            .IsRequired();

        user.Property(x => x.CreatedAt)
            .IsRequired();


        var match = modelBuilder.Entity<GameMatch>();

        match.ToTable("Matches");

        match.HasKey(x => x.Id);

        match.Property(x => x.BoardSize)
            .IsRequired();

        match.Property(x => x.WinLength)
            .IsRequired();

        match.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        match.Property(x => x.CreatedAt)
            .IsRequired();

        match.HasOne(x => x.OwnerUser)
            .WithMany()
            .HasForeignKey(x => x.OwnerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        match.HasOne(x => x.OpponentUser)
            .WithMany()
            .HasForeignKey(x => x.OpponentUserId)
            .OnDelete(DeleteBehavior.Restrict);

        match.HasOne(x => x.WinnerUser)
            .WithMany()
            .HasForeignKey(x => x.WinnerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        var move = modelBuilder.Entity<MatchMove>();

        move.ToTable("MatchMoves");

        move.HasKey(x => x.Id);

        move.Property(x => x.Row)
            .IsRequired();

        move.Property(x => x.Column)
            .IsRequired();

        move.Property(x => x.MoveNumber)
            .IsRequired();

        move.Property(x => x.Symbol)
            .HasMaxLength(1)
            .IsRequired();

        move.Property(x => x.CreatedAt)
            .IsRequired();

        move.HasOne(x => x.Match)
            .WithMany(x => x.Moves)
            .HasForeignKey(x => x.MatchId)
            .OnDelete(DeleteBehavior.Cascade);

        move.HasOne(x => x.PlayerUser)
            .WithMany()
            .HasForeignKey(x => x.PlayerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        move.HasIndex(x => new
        {
            x.MatchId,
            x.Row,
            x.Column
        })
        .IsUnique();

        move.HasIndex(x => new
        {
            x.MatchId,
            x.MoveNumber
        })
        .IsUnique();

        var matchFinishedEvent = modelBuilder.Entity<MatchFinishedEventRecord>();

        matchFinishedEvent.ToTable("MatchFinishedEvents");

        matchFinishedEvent.HasKey(x => x.Id);

        matchFinishedEvent.Property(x => x.EventId)
            .IsRequired();

        matchFinishedEvent.Property(x => x.MatchId)
            .IsRequired();

        matchFinishedEvent.Property(x => x.IsDraw)
            .IsRequired();

        matchFinishedEvent.Property(x => x.FinishedAt)
            .IsRequired();

        matchFinishedEvent.Property(x => x.ProcessedAt)
            .IsRequired();

        matchFinishedEvent
            .HasIndex(x => x.EventId)
            .IsUnique();

        matchFinishedEvent
            .HasIndex(x => x.MatchId);
    }
}