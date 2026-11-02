using Microsoft.EntityFrameworkCore;
using Portfolio.Api.Data;
using Portfolio.Api.DTOs.Blogposts;
using Portfolio.Api.Entities;

namespace Portfolio.Api.Services;

public class BlogpostService(PortfolioDbContext dbContext) : IBlogpostService
{
    public async Task<IReadOnlyList<BlogpostDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var blogposts = await dbContext.Blogposts
            .AsNoTracking()
            .OrderByDescending(blogpost => blogpost.Publicatiedatum)
            .ThenBy(blogpost => blogpost.Id)
            .ToListAsync(cancellationToken);

        return blogposts.Select(MapToDto).ToList();
    }

    public async Task<BlogpostDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var blogpost = await dbContext.Blogposts
            .AsNoTracking()
            .FirstOrDefaultAsync(blogpost => blogpost.Id == id, cancellationToken);

        return blogpost is null ? null : MapToDto(blogpost);
    }

    public async Task<BlogpostDto> CreateAsync(
        CreateBlogpostDto dto,
        CancellationToken cancellationToken = default)
    {
        var blogpost = new Blogpost
        {
            Titel = dto.Titel,
            Inhoud = dto.Inhoud,
            Publicatiedatum = dto.Publicatiedatum!.Value
        };

        dbContext.Blogposts.Add(blogpost);
        await dbContext.SaveChangesAsync(cancellationToken);

        return MapToDto(blogpost);
    }

    public async Task<bool> UpdateAsync(
        int id,
        UpdateBlogpostDto dto,
        CancellationToken cancellationToken = default)
    {
        var blogpost = await dbContext.Blogposts
            .FirstOrDefaultAsync(blogpost => blogpost.Id == id, cancellationToken);

        if (blogpost is null)
        {
            return false;
        }

        blogpost.Titel = dto.Titel;
        blogpost.Inhoud = dto.Inhoud;
        blogpost.Publicatiedatum = dto.Publicatiedatum!.Value;

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var blogpost = await dbContext.Blogposts
            .FirstOrDefaultAsync(blogpost => blogpost.Id == id, cancellationToken);

        if (blogpost is null)
        {
            return false;
        }

        dbContext.Blogposts.Remove(blogpost);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static BlogpostDto MapToDto(Blogpost blogpost)
    {
        return new BlogpostDto
        {
            Id = blogpost.Id,
            Titel = blogpost.Titel,
            Inhoud = blogpost.Inhoud,
            Publicatiedatum = blogpost.Publicatiedatum
        };
    }
}
