using System.ComponentModel.DataAnnotations;

namespace Portfolio.Api.DTOs.Projects;

public class UpdateProjectDto
{
    [Required]
    [StringLength(150)]
    public string Titel { get; set; } = string.Empty;

    [Required]
    [StringLength(4000)]
    public string Beschrijving { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Categorie { get; set; } = string.Empty;

    [StringLength(2048)]
    [Url]
    public string? GitHubUrl { get; set; }

    [Required]
    public DateTime? Datum { get; set; }
}
