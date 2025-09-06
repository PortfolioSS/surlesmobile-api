using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace SurlesMobile.Api.Extensions;

public static class EndpointExtensions
{
    public static WebApplication MapPortfolioEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/portfolio")
                      .WithTags("Portfolio")
                      .WithOpenApi();

        // Get portfolio data
        group.MapGet("", GetPortfolio)
             .WithName("GetPortfolio")
             .WithSummary("Get portfolio data for a site")
             .RequireAuthorization("Read");

        // Get specific site
        group.MapGet("/sites/{slug}", GetSite)
             .WithName("GetSite")
             .WithSummary("Get site information by slug")
             .RequireAuthorization("Read");

        // Update site
        group.MapPut("/sites/{slug}", UpdateSite)
             .WithName("UpdateSite")
             .WithSummary("Update site information")
             .RequireAuthorization("Write");

        return app;
    }

    public static WebApplication MapSectionEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/sections")
                      .WithTags("Sections")
                      .WithOpenApi();

        // Get sections
        group.MapGet("", GetSections)
             .WithName("GetSections")
             .WithSummary("Get all sections")
             .RequireAuthorization("Read");

        // Get section by id
        group.MapGet("/{id:guid}", GetSection)
             .WithName("GetSection")
             .WithSummary("Get section by ID")
             .RequireAuthorization("Read");

        // Create section
        group.MapPost("", CreateSection)
             .WithName("CreateSection")
             .WithSummary("Create a new section")
             .RequireAuthorization("Write");

        // Update section
        group.MapPut("/{id:guid}", UpdateSection)
             .WithName("UpdateSection")
             .WithSummary("Update a section")
             .RequireAuthorization("Write");

        // Delete section
        group.MapDelete("/{id:guid}", DeleteSection)
             .WithName("DeleteSection")
             .WithSummary("Delete a section")
             .RequireAuthorization("Admin");

        return app;
    }

    public static WebApplication MapItemEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/items")
                      .WithTags("Items")
                      .WithOpenApi();

        // Get items
        group.MapGet("", GetItems)
             .WithName("GetItems")
             .WithSummary("Get all items")
             .RequireAuthorization("Read");

        // Get item by id
        group.MapGet("/{id:guid}", GetItem)
             .WithName("GetItem")
             .WithSummary("Get item by ID")
             .RequireAuthorization("Read");

        // Create item
        group.MapPost("", CreateItem)
             .WithName("CreateItem")
             .WithSummary("Create a new item")
             .RequireAuthorization("Write");

        // Update item
        group.MapPut("/{id:guid}", UpdateItem)
             .WithName("UpdateItem")
             .WithSummary("Update an item")
             .RequireAuthorization("Write");

        // Delete item
        group.MapDelete("/{id:guid}", DeleteItem)
             .WithName("DeleteItem")
             .WithSummary("Delete an item")
             .RequireAuthorization("Admin");

        return app;
    }

    public static WebApplication MapHealthEndpoints(this WebApplication app)
    {
        // Health check endpoints
        app.MapHealthChecks("/health");
        app.MapHealthChecks("/health/ready", new()
        {
            Predicate = check => check.Tags.Contains("ready")
        });
        app.MapHealthChecks("/health/live", new()
        {
            Predicate = _ => false
        });

        return app;
    }

    // Portfolio endpoints implementation
    private static async Task<IResult> GetPortfolio(string? site, PortfolioDb db)
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
    }

    private static async Task<IResult> GetSite(string slug, PortfolioDb db)
    {
        var site = await db.Sites.FirstOrDefaultAsync(s => s.Slug == slug);
        return site == null ? Results.NotFound() : Results.Ok(site);
    }

    private static async Task<IResult> UpdateSite(string slug, [FromBody] UpdateSiteRequest request, PortfolioDb db)
    {
        var site = await db.Sites.FirstOrDefaultAsync(s => s.Slug == slug);
        if (site == null) return Results.NotFound();

        site.Title = request.Title ?? site.Title;
        site.Tagline = request.Tagline ?? site.Tagline;
        site.Email = request.Email ?? site.Email;
        site.Location = request.Location ?? site.Location;
        site.Linkedin = request.Linkedin ?? site.Linkedin;
        site.Github = request.Github ?? site.Github;
        site.HeroHeadline = request.HeroHeadline ?? site.HeroHeadline;
        site.HeroSubhead = request.HeroSubhead ?? site.HeroSubhead;

        await db.SaveChangesAsync();
        return Results.Ok(site);
    }

    // Section endpoints implementation
    private static async Task<IResult> GetSections(PortfolioDb db, string? siteSlug = "surlesmobile")
    {
        var site = await db.Sites.FirstOrDefaultAsync(s => s.Slug == siteSlug);
        if (site == null) return Results.NotFound(new { error = "Site not found" });

        var sections = await db.Sections
            .Where(s => s.SiteId == site.Id)
            .OrderBy(s => s.SortOrder)
            .Include(s => s.Items.OrderBy(i => i.SortOrder))
            .ToListAsync();

        return Results.Ok(sections);
    }

    private static async Task<IResult> GetSection(Guid id, PortfolioDb db)
    {
        var section = await db.Sections
            .Include(s => s.Items.OrderBy(i => i.SortOrder))
            .FirstOrDefaultAsync(s => s.Id == id);

        return section == null ? Results.NotFound() : Results.Ok(section);
    }

    private static async Task<IResult> CreateSection([FromBody] CreateSectionRequest request, PortfolioDb db)
    {
        var site = await db.Sites.FirstOrDefaultAsync(s => s.Slug == request.SiteSlug ?? "surlesmobile");
        if (site == null) return Results.NotFound(new { error = "Site not found" });

        var section = new Section
        {
            Id = Guid.NewGuid(),
            SiteId = site.Id,
            SectionKey = request.Key,
            Title = request.Title,
            Icon = request.Icon,
            SortOrder = request.SortOrder ?? 0
        };

        db.Sections.Add(section);
        await db.SaveChangesAsync();

        return Results.Created($"/api/sections/{section.Id}", section);
    }

    private static async Task<IResult> UpdateSection(Guid id, [FromBody] UpdateSectionRequest request, PortfolioDb db)
    {
        var section = await db.Sections.FindAsync(id);
        if (section == null) return Results.NotFound();

        section.Title = request.Title ?? section.Title;
        section.Icon = request.Icon ?? section.Icon;
        section.SortOrder = request.SortOrder ?? section.SortOrder;

        await db.SaveChangesAsync();
        return Results.Ok(section);
    }

    private static async Task<IResult> DeleteSection(Guid id, PortfolioDb db)
    {
        var section = await db.Sections.FindAsync(id);
        if (section == null) return Results.NotFound();

        db.Sections.Remove(section);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    // Item endpoints implementation
    private static async Task<IResult> GetItems(PortfolioDb db, Guid? sectionId = null)
    {
        var query = db.Items.AsQueryable();
        
        if (sectionId.HasValue)
        {
            query = query.Where(i => i.SectionId == sectionId.Value);
        }

        var items = await query.OrderBy(i => i.SortOrder).ToListAsync();
        return Results.Ok(items);
    }

    private static async Task<IResult> GetItem(Guid id, PortfolioDb db)
    {
        var item = await db.Items.FindAsync(id);
        return item == null ? Results.NotFound() : Results.Ok(item);
    }

    private static async Task<IResult> CreateItem([FromBody] CreateItemRequest request, PortfolioDb db)
    {
        var section = await db.Sections.FirstOrDefaultAsync(s => s.SectionKey == request.SectionKey);
        if (section == null) return Results.NotFound(new { error = "Section not found" });

        var item = new Item
        {
            Id = Guid.NewGuid(),
            SectionId = section.Id,
            Title = request.Title,
            Description = request.Description,
            Href = request.Href,
            Meta = request.Meta,
            SortOrder = request.SortOrder ?? 0
        };

        db.Items.Add(item);
        await db.SaveChangesAsync();

        return Results.Created($"/api/items/{item.Id}", item);
    }

    private static async Task<IResult> UpdateItem(Guid id, [FromBody] UpdateItemRequest request, PortfolioDb db)
    {
        var item = await db.Items.FindAsync(id);
        if (item == null) return Results.NotFound();

        item.Title = request.Title ?? item.Title;
        item.Description = request.Description ?? item.Description;
        item.Href = request.Href ?? item.Href;
        item.Meta = request.Meta ?? item.Meta;
        item.SortOrder = request.SortOrder ?? item.SortOrder;

        await db.SaveChangesAsync();
        return Results.Ok(item);
    }

    private static async Task<IResult> DeleteItem(Guid id, PortfolioDb db)
    {
        var item = await db.Items.FindAsync(id);
        if (item == null) return Results.NotFound();

        db.Items.Remove(item);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }
}

// Request DTOs
public record CreateSectionRequest(string Key, string Title, string? Icon, int? SortOrder, string? SiteSlug);
public record UpdateSectionRequest(string? Title, string? Icon, int? SortOrder);
public record CreateItemRequest(string SectionKey, string Title, string? Description, string? Href, string? Meta, int? SortOrder);
public record UpdateItemRequest(string? Title, string? Description, string? Href, string? Meta, int? SortOrder);
public record UpdateSiteRequest(string? Title, string? Tagline, string? Email, string? Location, string? Linkedin, string? Github, string? HeroHeadline, string? HeroSubhead);