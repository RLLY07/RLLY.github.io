using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portfolio.Api.DTOs.Projects;
using Portfolio.Api.Services;

namespace Portfolio.Api.Controllers;

[ApiController]
[Route("api/projects")]
public class ProjectsController(IProjectService projectService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProjectDto>>> GetAll(CancellationToken cancellationToken)
    {
        var projects = await projectService.GetAllAsync(cancellationToken);
        return Ok(projects);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProjectDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var project = await projectService.GetByIdAsync(id, cancellationToken);
        return project is null ? NotFound() : Ok(project);
    }

    [HttpPost]
    [Authorize(Policy = PortfolioWritePolicy.Name)]
    public async Task<ActionResult<ProjectDto>> Create(
        CreateProjectDto dto,
        CancellationToken cancellationToken)
    {
        var project = await projectService.CreateAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = project.Id }, project);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = PortfolioWritePolicy.Name)]
    public async Task<IActionResult> Update(
        int id,
        UpdateProjectDto dto,
        CancellationToken cancellationToken)
    {
        var updated = await projectService.UpdateAsync(id, dto, cancellationToken);
        return updated ? NoContent() : NotFound();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = PortfolioWritePolicy.Name)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await projectService.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
