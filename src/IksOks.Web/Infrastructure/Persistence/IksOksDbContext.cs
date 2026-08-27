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
    }
}