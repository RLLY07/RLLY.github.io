using Microsoft.EntityFrameworkCore;
using Portfolio.Api.Entities;

namespace Portfolio.Api.Data;

public class PortfolioDbContext(DbContextOptions<PortfolioDbContext> options) : DbContext(options)
{
    public DbSet<Project> Projects => Set<Project>();

    public DbSet<Blogpost> Blogposts => Set<Blogpost>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Project>(entity =>
        {
            entity.Property(project => project.Titel).HasMaxLength(150).IsRequired();
            entity.Property(project => project.Beschrijving).HasMaxLength(4000).IsRequired();
            entity.Property(project => project.Categorie).HasMaxLength(100).IsRequired();
            entity.Property(project => project.GitHubUrl).HasMaxLength(2048);
            entity.Property(project => project.Datum).IsRequired();
        });

        modelBuilder.Entity<Blogpost>(entity =>
        {
            entity.Property(blogpost => blogpost.Titel).HasMaxLength(200).IsRequired();
            entity.Property(blogpost => blogpost.Inhoud).IsRequired();
            entity.Property(blogpost => blogpost.Publicatiedatum).IsRequired();
        });
    }
}
