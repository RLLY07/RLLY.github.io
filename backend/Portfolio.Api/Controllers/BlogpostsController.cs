using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portfolio.Api.DTOs.Blogposts;
using Portfolio.Api.Services;

namespace Portfolio.Api.Controllers;

[ApiController]
[Route("api/blogposts")]
public class BlogpostsController(IBlogpostService blogpostService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BlogpostDto>>> GetAll(CancellationToken cancellationToken)
    {
        var blogposts = await blogpostService.GetAllAsync(cancellationToken);
        return Ok(blogposts);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<BlogpostDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var blogpost = await blogpostService.GetByIdAsync(id, cancellationToken);
        return blogpost is null ? NotFound() : Ok(blogpost);
    }

    [HttpPost]
    [Authorize(Policy = PortfolioWritePolicy.Name)]
    public async Task<ActionResult<BlogpostDto>> Create(
        CreateBlogpostDto dto,
        CancellationToken cancellationToken)
    {
        var blogpost = await blogpostService.CreateAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = blogpost.Id }, blogpost);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = PortfolioWritePolicy.Name)]
    public async Task<IActionResult> Update(
        int id,
        UpdateBlogpostDto dto,
        CancellationToken cancellationToken)
    {
        var updated = await blogpostService.UpdateAsync(id, dto, cancellationToken);
        return updated ? NoContent() : NotFound();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = PortfolioWritePolicy.Name)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await blogpostService.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
