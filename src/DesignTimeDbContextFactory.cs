using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<PortfolioDb>
{
  public PortfolioDb CreateDbContext(string[] args)
  {
    var cs = Environment.GetEnvironmentVariable("ConnectionStrings__Default")
      ?? "Host=localhost;Port=5432;Database=portfolio;Username=portfolio;Password=portfolio";
    var opts = new DbContextOptionsBuilder<PortfolioDb>().UseNpgsql(cs).Options;
    return new PortfolioDb(opts);
  }
}
