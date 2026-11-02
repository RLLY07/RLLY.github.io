using Microsoft.EntityFrameworkCore;
using Portfolio.Api.Data;
using Portfolio.Api.DTOs.Projects;
using Portfolio.Api.Entities;

namespace Portfolio.Api.Services;

public class ProjectService(PortfolioDbContext dbContext) : IProjectService
{
    public async Task<IReadOnlyList<ProjectDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var projects = await dbContext.Projects
            .AsNoTracking()
            .OrderBy(project => project.Datum)
            .ThenBy(project => project.Id)
            .ToListAsync(cancellationToken);

        return projects.Select(MapToDto).ToList();
    }

    public async Task<ProjectDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var project = await dbContext.Projects
            .AsNoTracking()
            .FirstOrDefaultAsync(project => project.Id == id, cancellationToken);

        return project is null ? null : MapToDto(project);
    }

    public async Task<ProjectDto> CreateAsync(
        CreateProjectDto dto,
        CancellationToken cancellationToken = default)
    {
        var project = new Project
        {
            Titel = dto.Titel,
            Beschrijving = dto.Beschrijving,
            Categorie = dto.Categorie,
            GitHubUrl = dto.GitHubUrl,
            Datum = dto.Datum!.Value
        };

        dbContext.Projects.Add(project);
        await dbContext.SaveChangesAsync(cancellationToken);

        return MapToDto(project);
    }

    public async Task<bool> UpdateAsync(
        int id,
        UpdateProjectDto dto,
        CancellationToken cancellationToken = default)
    {
        var project = await dbContext.Projects
            .FirstOrDefaultAsync(project => project.Id == id, cancellationToken);

        if (project is null)
        {
            return false;
        }

        project.Titel = dto.Titel;
        project.Beschrijving = dto.Beschrijving;
        project.Categorie = dto.Categorie;
        project.GitHubUrl = dto.GitHubUrl;
        project.Datum = dto.Datum!.Value;

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var project = await dbContext.Projects
            .FirstOrDefaultAsync(project => project.Id == id, cancellationToken);

        if (project is null)
        {
            return false;
        }

        dbContext.Projects.Remove(project);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static ProjectDto MapToDto(Project project)
    {
        return new ProjectDto
        {
            Id = project.Id,
            Titel = project.Titel,
            Beschrijving = project.Beschrijving,
            Categorie = project.Categorie,
            GitHubUrl = project.GitHubUrl,
            Datum = project.Datum
        };
    }
}
