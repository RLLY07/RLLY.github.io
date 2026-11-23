using System.ComponentModel.DataAnnotations;

namespace Portfolio.Api.DTOs.Auth;

public class RegisterRequestDto
{
    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(256, MinimumLength = 12)]
    public string Password { get; set; } = string.Empty;
}
