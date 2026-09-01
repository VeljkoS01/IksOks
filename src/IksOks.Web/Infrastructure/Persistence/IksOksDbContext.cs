using IksOks.Web.Domain.Entities;
using Microsoft.EntityFrameworkCore;

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
    }
}