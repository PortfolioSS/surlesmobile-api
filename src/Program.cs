using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var cs = builder.Configuration.GetConnectionString("Default")
         ?? Environment.GetEnvironmentVariable("ConnectionStrings__Default");
var issuer = Environment.GetEnvironmentVariable("COGNITO__ISSUER");
var audience = Environment.GetEnvironmentVariable("COGNITO__AUDIENCE");

builder.Services.AddDbContextPool<PortfolioDb>(o => o.UseNpgsql(cs));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
  .AddJwtBearer(o => {
    o.Authority = issuer;
    o.TokenValidationParameters = new TokenValidationParameters { ValidAudience = audience };
  });

builder.Services.AddAuthorization(opt => {
  opt.AddPolicy("Read",  p => p.RequireAssertion(c =>
    c.User.IsInRole("viewer") || c.User.IsInRole("editor") || c.User.IsInRole("admin") ||
    c.User.HasClaim("scope", s => s.Split(' ').Contains("portfolio/read"))));
  opt.AddPolicy("Write", p => p.RequireAssertion(c =>
    c.User.IsInRole("editor") || c.User.IsInRole("admin") ||
    c.User.HasClaim("scope", s => s.Split(' ').Contains("portfolio/write"))));
  opt.AddPolicy("Admin", p => p.RequireAssertion(c =>
    c.User.IsInRole("admin") || c.User.HasClaim("scope", s => s.Split(' ').Contains("portfolio/admin"))));
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new() { Title = "SurlesMobile API", Version = "v1" });
    o.AddSecurityDefinition("Bearer", new() {
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Enter: Bearer {token}"
    });
    o.AddSecurityRequirement(new() {
      { new() { Reference = new() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" } }, new string[] {} }
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
  var db = scope.ServiceProvider.GetRequiredService<PortfolioDb>();
  await db.Database.MigrateAsync();
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "SurlesMobile API v1");
    c.RoutePrefix = "swagger";
});

app.MapGet("/health", () => Results.Ok(new { ok = true }));

app.MapGet("/portfolio", async (string? site, PortfolioDb db) =>
{
    var slug = string.IsNullOrWhiteSpace(site) ? "surlesmobile" : site;
    var s = await db.Sites
        .Include(x => x.Ctas.OrderBy(c => c.SortOrder))
        .Include(x => x.Sections.OrderBy(sec => sec.SortOrder))
            .ThenInclude(sec => sec.Items.OrderBy(i => i.SortOrder))
        .FirstOrDefaultAsync(x => x.Slug == slug);
    if (s == null) return Results.NotFound();

    var dto = new PortfolioDto(
        new SiteDto(s.Title, s.Tagline, s.Email, s.Location, new SocialDto(s.Linkedin, s.Github)),
        new HeroDto(s.HeroHeadline ?? "Software Architect & Builder", s.HeroSubhead,
            s.Ctas.Select(c => new HeroCtaDto(c.Label, c.Href, c.Icon)).ToList()),
        s.Sections.Select(sec => new SectionDto(
            sec.SectionKey, sec.Title, sec.Icon,
            sec.Items.Select(i => new LinkItemDto(i.Title, i.Description, i.Href, i.Meta)).ToList()
        )).ToList()
    );
    return Results.Json(dto);
})
.WithName("GetPortfolio")
.WithTags("Portfolio")
.WithOpenApi(o => { o.Summary = "Get portfolio data"; return o; })
.RequireAuthorization("Read");

app.MapPost("/sections", async (CreateSection req, PortfolioDb db) =>
{
  var site = await db.Sites.FirstAsync(s => s.Slug == "surlesmobile");
  var sec = new Section { Id = Guid.NewGuid(), SiteId = site.Id, SectionKey = req.Key, Title = req.Title, Icon = req.Icon, SortOrder = req.SortOrder ?? 0 };
  db.Sections.Add(sec); await db.SaveChangesAsync();
  return Results.Created($"/sections/{sec.Id}", new { sec.Id });
}).RequireAuthorization("Write");

app.MapPost("/items", async (CreateItem req, PortfolioDb db) =>
{
  var sec = await db.Sections.FirstAsync(s => s.SectionKey == req.SectionKey);
  var it = new Item { Id = Guid.NewGuid(), SectionId = sec.Id, Title = req.Title, Description = req.Description, Href = req.Href, Meta = req.Meta, SortOrder = req.SortOrder ?? 0 };
  db.Items.Add(it); await db.SaveChangesAsync();
  return Results.Created($"/items/{it.Id}", new { it.Id });
}).RequireAuthorization("Write");

app.MapDelete("/items/{id:guid}", async (Guid id, PortfolioDb db) =>
{
  var it = await db.Items.FindAsync(id); if (it is null) return Results.NotFound();
  db.Items.Remove(it); await db.SaveChangesAsync();
  return Results.NoContent();
}).RequireAuthorization("Admin");

app.Run();

public record CreateSection(string Key, string Title, string? Icon, int? SortOrder);
public record CreateItem(string SectionKey, string Title, string? Description, string? Href, string? Meta, int? SortOrder);
