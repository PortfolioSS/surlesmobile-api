using Microsoft.EntityFrameworkCore;
using SurlesMobile.Api.Extensions;
using SurlesMobile.Api.Middleware;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

// Add services
builder.Services.AddSerilogLogging();
builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddAwsServices();
builder.Services.AddJwtAuthentication();
builder.Services.AddCustomAuthorization();
builder.Services.AddCustomHealthChecks();
builder.Services.AddCustomCors(builder.Configuration);
builder.Services.AddCustomSwagger();

var app = builder.Build();

// Configure pipeline
app.UseMiddleware<GlobalExceptionMiddleware>();

// Apply database migrations
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<PortfolioDb>();
    await db.Database.MigrateAsync();
    
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Database migrations applied successfully");
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "Failed to apply database migrations");
    throw;
}

// Configure middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "SurlesMobile API v1");
        options.RoutePrefix = "swagger";
        options.DisplayRequestDuration();
    });
}

app.UseCors("AllowAngularApp");
app.UseAuthentication();
app.UseAuthorization();

// Map endpoints
app.MapHealthEndpoints();
app.MapPortfolioEndpoints();
app.MapSectionEndpoints();
app.MapItemEndpoints();

// Legacy endpoints for backward compatibility
app.MapGet("/health", () => Results.Ok(new { 
    status = "healthy", 
    timestamp = DateTime.UtcNow,
    version = "1.0.0"
})).WithTags("Health");

app.MapGet("/portfolio", async (string? site, PortfolioDb db) =>
{
    var slug = string.IsNullOrWhiteSpace(site) ? "surlesmobile" : site;
    var s = await db.Sites
        .Include(x => x.Ctas.OrderBy(c => c.SortOrder))
        .Include(x => x.Sections.OrderBy(sec => sec.SortOrder))
            .ThenInclude(sec => sec.Items.OrderBy(i => i.SortOrder))
        .FirstOrDefaultAsync(x => x.Slug == slug);

    if (s == null) return Results.NotFound(new { error = "Site not found" });

    var dto = new PortfolioDto(
        new SiteDto(s.Title, s.Tagline, s.Email, s.Location, new SocialDto(s.Linkedin, s.Github)),
        new HeroDto(s.HeroHeadline ?? "Software Architect & Builder", s.HeroSubhead,
            s.Ctas.Select(c => new HeroCtaDto(c.Label, c.Href, c.Icon)).ToList()),
        s.Sections.Select(sec => new SectionDto(
            sec.SectionKey, sec.Title, sec.Icon,
            sec.Items.Select(i => new LinkItemDto(i.Title, i.Description, i.Href, i.Meta)).ToList()
        )).ToList()
    );
    
    return Results.Ok(dto);
})
.WithName("GetPortfolioLegacy")
.WithTags("Portfolio")
.RequireAuthorization("Read");

app.Run();

// Legacy DTOs for backward compatibility
public record CreateSection(string Key, string Title, string? Icon, int? SortOrder);
public record CreateItem(string SectionKey, string Title, string? Description, string? Href, string? Meta, int? SortOrder);
