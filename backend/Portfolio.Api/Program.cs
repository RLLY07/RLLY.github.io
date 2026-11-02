using Microsoft.EntityFrameworkCore;
using Portfolio.Api.Data;
using Portfolio.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("Portfolio")
    ?? throw new InvalidOperationException("Connection string 'Portfolio' is not configured.");

builder.Services.AddDbContext<PortfolioDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IBlogpostService, BlogpostService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();
