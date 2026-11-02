using Portfolio.Api.DTOs.Blogposts;

namespace Portfolio.Api.Services;

public interface IBlogpostService
{
    Task<IReadOnlyList<BlogpostDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<BlogpostDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<BlogpostDto> CreateAsync(CreateBlogpostDto dto, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(int id, UpdateBlogpostDto dto, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
