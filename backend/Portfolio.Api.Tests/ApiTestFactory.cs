using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Portfolio.Api.Data;
using Portfolio.Api.Entities;

namespace Portfolio.Api.Tests;

internal sealed class ApiTestFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");

    public ApiTestFactory()
    {
        connection.Open();
        var options = new DbContextOptionsBuilder<PortfolioDbContext>()
            .UseSqlite(connection)
            .Options;

        using var dbContext = new PortfolioDbContext(options);
        dbContext.Database.EnsureCreated();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IDbContextOptionsConfiguration<PortfolioDbContext>>();
            services.RemoveAll<DbContextOptions<PortfolioDbContext>>();
            services.RemoveAll<PortfolioDbContext>();
            services.AddDbContext<PortfolioDbContext>(options => options.UseSqlite(connection));
        });
    }

    public async Task SeedPortfolioAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PortfolioDbContext>();
        dbContext.Projects.Add(new Project
        {
            Titel = "Bestaand project",
            Beschrijving = "Project voor integratietests.",
            Categorie = "Test",
            GitHubUrl = "https://example.test/project",
            Datum = new DateTime(2026, 1, 2)
        });
        dbContext.Blogposts.Add(new Blogpost
        {
            Titel = "Bestaande blogpost",
            Inhoud = "Blogpost voor integratietests.",
            Publicatiedatum = new DateTime(2026, 1, 3)
        });
        await dbContext.SaveChangesAsync();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            connection.Dispose();
        }
    }
}
