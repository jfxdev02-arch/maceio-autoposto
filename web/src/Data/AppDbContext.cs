using Microsoft.EntityFrameworkCore;
using MaceioWeb.Models;

namespace MaceioWeb.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Respondent> Respondents => Set<Respondent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Respondent>(entity =>
        {
            entity.ToTable("Respondents");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PhoneNumber).IsUnique();
            entity.HasIndex(e => e.LuckyNumber).IsUnique();
        });
    }
}
