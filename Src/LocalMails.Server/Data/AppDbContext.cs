using Microsoft.EntityFrameworkCore;

namespace LocalMails.Server.Data;

public class AppDbContext : DbContext {
    // TODO: Implement DBSets

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        base.OnModelCreating(modelBuilder);
        // TODO: Implement OnModelCreating
    }
}