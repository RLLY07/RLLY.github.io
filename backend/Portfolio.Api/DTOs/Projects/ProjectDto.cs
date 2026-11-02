namespace Portfolio.Api.DTOs.Projects;

public class ProjectDto
{
    public int Id { get; set; }

    public string Titel { get; set; } = string.Empty;

    public string Beschrijving { get; set; } = string.Empty;

    public string Categorie { get; set; } = string.Empty;

    public string? GitHubUrl { get; set; }

    public DateTime Datum { get; set; }
}
