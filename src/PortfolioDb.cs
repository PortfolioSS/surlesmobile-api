using Microsoft.EntityFrameworkCore;

public class PortfolioDb : DbContext
{
    public PortfolioDb(DbContextOptions<PortfolioDb> options) : base(options) {}
    public DbSet<Site> Sites => Set<Site>();
    public DbSet<Cta> Ctas => Set<Cta>();
    public DbSet<Section> Sections => Set<Section>();
    public DbSet<Item> Items => Set<Item>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Site>(e => { e.HasIndex(x => x.Slug).IsUnique(); e.Property(x => x.Title).IsRequired(); });
        b.Entity<Section>(e => {
            e.HasIndex(x => new { x.SiteId, x.SectionKey }).IsUnique();
            e.HasOne(x => x.Site).WithMany(s => s.Sections).HasForeignKey(x => x.SiteId).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<Item>(e => { e.HasOne(x => x.Section).WithMany(s => s.Items).HasForeignKey(x => x.SectionId).OnDelete(DeleteBehavior.Cascade); });
        b.Entity<Cta>(e => { e.HasOne(x => x.Site).WithMany(s => s.Ctas).HasForeignKey(x => x.SiteId).OnDelete(DeleteBehavior.Cascade); });
    }
}
