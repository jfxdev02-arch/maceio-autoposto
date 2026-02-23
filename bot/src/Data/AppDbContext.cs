using Microsoft.EntityFrameworkCore;
using MaceioBot.Models;

namespace MaceioBot.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Respondent> Respondents => Set<Respondent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Respondent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PhoneNumber).IsUnique();
            entity.HasIndex(e => e.LuckyNumber).IsUnique();
            
            entity.Property(e => e.PhoneNumber).HasMaxLength(20).IsRequired();
            entity.Property(e => e.PushName).HasMaxLength(100);
            entity.Property(e => e.CurrentStep).HasMaxLength(20).IsRequired();
            entity.Property(e => e.FrequencyAnswer).HasMaxLength(50);
            entity.Property(e => e.ConvenienceAnswer).HasMaxLength(10);
            entity.Property(e => e.FuelAnswer).HasMaxLength(30);
            entity.Property(e => e.RatingAnswer).HasMaxLength(20);
            entity.Property(e => e.LuckyNumber).HasMaxLength(6);
            entity.Property(e => e.Source).HasMaxLength(20).IsRequired();
        });
    }
}
