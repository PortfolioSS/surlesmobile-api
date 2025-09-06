using Microsoft.EntityFrameworkCore;

public class Site
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string? Tagline { get; set; }
    public string? Email { get; set; }
    public string? Location { get; set; }
    public string? Linkedin { get; set; }
    public string? Github { get; set; }
    public string? HeroHeadline { get; set; }
    public string? HeroSubhead { get; set; }
    public List<Cta> Ctas { get; set; } = new();
    public List<Section> Sections { get; set; } = new();
}

public class Cta
{
    public Guid Id { get; set; }
    public Guid SiteId { get; set; }
    public Site Site { get; set; } = default!;
    public string Label { get; set; } = default!;
    public string Href { get; set; } = default!;
    public string? Icon { get; set; }
    public int SortOrder { get; set; }
}

public class Section
{
    public Guid Id { get; set; }
    public Guid SiteId { get; set; }
    public Site Site { get; set; } = default!;
    public string SectionKey { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string? Icon { get; set; }
    public int SortOrder { get; set; }
    public List<Item> Items { get; set; } = new();
}

public class Item
{
    public Guid Id { get; set; }
    public Guid SectionId { get; set; }
    public Section Section { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public string? Href { get; set; }
    public string? Meta { get; set; }
    public int SortOrder { get; set; }
}

public record LinkItemDto(string Title, string? Description, string? Href, string? Meta);
public record SectionDto(string Key, string Title, string? Icon, List<LinkItemDto> Items);
public record HeroCtaDto(string Label, string Href, string? Icon);
public record PortfolioDto(SiteDto Site, HeroDto Hero, List<SectionDto> Sections);
public record SiteDto(string Title, string? Tagline, string? Email, string? Location, SocialDto Social);
public record SocialDto(string? Linkedin, string? Github);
public record HeroDto(string Headline, string? Subhead, List<HeroCtaDto> Cta);
