using System.ComponentModel.DataAnnotations;

namespace Portfolio.Api.DTOs.Blogposts;

public class UpdateBlogpostDto
{
    [Required]
    [StringLength(200)]
    public string Titel { get; set; } = string.Empty;

    [Required]
    public string Inhoud { get; set; } = string.Empty;

    [Required]
    public DateTime? Publicatiedatum { get; set; }
}
