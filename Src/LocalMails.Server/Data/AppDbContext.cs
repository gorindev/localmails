using LocalMails.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace LocalMails.Server.Data;

public class AppDbContext : DbContext
{
    public DbSet<EmailMessage> Messages { get; set; } = null!;

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<EmailMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.HasIndex(e => e.ReceivedDate).IsDescending();
        });
    }
}
