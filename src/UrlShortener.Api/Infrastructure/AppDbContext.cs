using Microsoft.EntityFrameworkCore;
using UrlShortener.Api.Domain;

namespace UrlShortener.Api.Infrastructure;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<ShortLink> ShortLinks => Set<ShortLink>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ShortLink>(entity =>
        {
            entity.HasKey(s => s.Id);

            entity.Property(s => s.Code)
                .HasMaxLength(7)
                .IsRequired();

            entity.Property(s => s.OriginalUrl)
                .HasMaxLength(2048)
                .IsRequired();

            entity.Property(s => s.CreatedAt)
                .HasDefaultValueSql("now()")
                .IsRequired();

            entity.HasIndex(s => s.Code)
                .IsUnique();

        });


    }
}
