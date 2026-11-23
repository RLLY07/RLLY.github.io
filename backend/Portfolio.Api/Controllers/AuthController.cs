using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Portfolio.Api.Data;
using Portfolio.Api.DTOs.Auth;
using Portfolio.Api.Services;

namespace Portfolio.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(
    UserManager<IdentityUser> userManager,
    SignInManager<IdentityUser> signInManager,
    PortfolioDbContext dbContext) : ControllerBase
{
    [HttpGet("csrf")]
    public IActionResult GetCsrfToken([FromServices] IAntiforgery antiforgery)
    {
        var tokens = antiforgery.GetAndStoreTokens(HttpContext);
        return Ok(new { requestToken = tokens.RequestToken });
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(
        RegisterRequestDto request,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var user = new IdentityUser
        {
            UserName = request.Email,
            Email = request.Email
        };

        var createResult = await userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            return BadRequest(new { errors = createResult.Errors.Select(error => error.Code) });
        }

        var roleResult = await userManager.AddToRoleAsync(user, "Gast");
        if (!roleResult.Succeeded)
        {
            return BadRequest(new { errors = roleResult.Errors.Select(error => error.Code) });
        }

        await transaction.CommitAsync(cancellationToken);
        return StatusCode(StatusCodes.Status201Created);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequestDto request)
    {
        var result = await signInManager.PasswordSignInAsync(
            request.Email,
            request.Password,
            isPersistent: false,
            lockoutOnFailure: true);

        if (!result.Succeeded)
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        return Ok(new { message = "Signed in." });
    }
}
