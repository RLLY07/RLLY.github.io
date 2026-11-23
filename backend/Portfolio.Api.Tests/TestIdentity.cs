using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Portfolio.Api.Services;

namespace Portfolio.Api.Tests;

internal sealed class TestIdentity : IDisposable
{
    private readonly WebApplicationFactory<Program> factory;

    public TestIdentity(WebApplicationFactory<Program> factory, string? role = null)
    {
        this.factory = factory;
        Client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false,
            HandleCookies = true
        });
        Email = $"test-{Guid.NewGuid():N}@example.test";
        Password = $"{Convert.ToHexString(RandomNumberGenerator.GetBytes(24))}aA1!";
        Role = role ?? "Gast";
    }

    public HttpClient Client { get; }
    public string Email { get; }
    public string Password { get; }
    public string Role { get; }

    public async Task RegisterAndLoginAsync()
    {
        var register = await Client.PostAsJsonAsync("/api/auth/register", new
        {
            email = Email,
            password = Password
        });
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);

        if (Role != "Gast")
        {
            await using var scope = factory.Services.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<RoleAdministrationService>()
                .SetRoleAsync(Email, Role);
        }

        var login = await Client.PostAsJsonAsync("/api/auth/login", new
        {
            email = Email,
            password = Password
        });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.Contains(login.Headers.GetValues("Set-Cookie"), value =>
            value.StartsWith("Portfolio.Auth=", StringComparison.Ordinal)
            && value.Contains("httponly", StringComparison.OrdinalIgnoreCase)
            && value.Contains("secure", StringComparison.OrdinalIgnoreCase));
    }

    public async Task<string> GetCsrfTokenAsync()
    {
        var response = await Client.GetAsync("/api/auth/csrf");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("requestToken").GetString()
            ?? throw new InvalidOperationException("The anti-forgery endpoint returned no token.");
    }

    public async Task<HttpResponseMessage> SendWriteAsync(
        HttpMethod method,
        string path,
        object? body,
        string? csrfToken)
    {
        using var request = new HttpRequestMessage(method, path);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        if (csrfToken is not null)
        {
            request.Headers.Add("X-CSRF-TOKEN", csrfToken);
        }

        return await Client.SendAsync(request);
    }

    public void Dispose() => Client.Dispose();
}
