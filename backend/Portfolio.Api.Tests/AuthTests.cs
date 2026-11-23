using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Portfolio.Api.Services;

namespace Portfolio.Api.Tests;

public class AuthTests
{
    [Theory]
    [InlineData("Editor")]
    [InlineData("Admin")]
    public async Task Registration_ignores_public_role_request_and_assigns_only_guest(string requestedRole)
    {
        // Arrange
        using var factory = new ApiTestFactory();
        using var client = CreateClient(factory);
        var email = $"signup-{Guid.NewGuid():N}@example.test";
        var password = StrongPassword();

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password,
            role = requestedRole,
            roles = new[] { "Gast", requestedRole },
            isAdmin = requestedRole == "Admin"
        });

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        await using var scope = factory.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var user = await userManager.FindByEmailAsync(email);
        Assert.NotNull(user);
        Assert.Equal(new[] { "Gast" }, await userManager.GetRolesAsync(user));
        Assert.DoesNotContain(await userManager.GetRolesAsync(user), role => role == "Editor" || role == "Admin");
    }

    [Fact]
    public async Task Registration_duplicate_returns_400_and_preserves_original_account()
    {
        // Arrange
        using var factory = new ApiTestFactory();
        using var client = CreateClient(factory);
        var email = $"duplicate-{Guid.NewGuid():N}@example.test";
        var password = StrongPassword();
        var payload = new { email, password };
        var first = await client.PostAsJsonAsync("/api/auth/register", payload);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        // Act
        var duplicate = await client.PostAsJsonAsync("/api/auth/register", payload);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, duplicate.StatusCode);
        await using var scope = factory.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        Assert.NotNull(await userManager.FindByEmailAsync(email));
    }

    [Theory]
    [InlineData("short")]
    [InlineData("longbutno1uppercase!")]
    public async Task Registration_weak_password_returns_400_without_creating_account(string password)
    {
        // Arrange
        using var factory = new ApiTestFactory();
        using var client = CreateClient(factory);
        var email = $"weak-{Guid.NewGuid():N}@example.test";

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/register", new { email, password });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await using var scope = factory.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        Assert.Null(await userManager.FindByEmailAsync(email));
    }

    [Fact]
    public async Task Registration_invalid_email_returns_400()
    {
        // Arrange
        using var factory = new ApiTestFactory();
        using var client = CreateClient(factory);

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "not-an-email",
            password = StrongPassword()
        });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Role_initializer_is_idempotent_for_all_three_roles()
    {
        // Arrange
        using var factory = new ApiTestFactory();
        using var client = CreateClient(factory);

        // Act
        await IdentityRoleInitializer.InitializeAsync(factory.Services);
        await IdentityRoleInitializer.InitializeAsync(factory.Services);

        // Assert
        await using var scope = factory.Services.CreateAsyncScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var roles = roleManager.Roles.Select(role => role.Name).OrderBy(name => name).ToArray();
        Assert.Equal(new[] { "Admin", "Editor", "Gast" }, roles);
    }

    [Fact]
    public async Task Login_sets_httponly_cookie_and_cookie_is_reused_for_authorization()
    {
        // Arrange
        using var factory = new ApiTestFactory();
        using var client = CreateClient(factory);
        var email = $"login-{Guid.NewGuid():N}@example.test";
        var password = StrongPassword();
        var register = await client.PostAsJsonAsync("/api/auth/register", new { email, password });
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);

        // Act
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password });

        // Assert
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var setCookie = Assert.Single(login.Headers.GetValues("Set-Cookie"));
        Assert.StartsWith("Portfolio.Auth=", setCookie, StringComparison.Ordinal);
        Assert.Contains("httponly", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", setCookie, StringComparison.OrdinalIgnoreCase);
        var body = await login.Content.ReadAsStringAsync();
        Assert.DoesNotContain(password, body, StringComparison.Ordinal);
        Assert.DoesNotContain(setCookie.Split(';')[0], body, StringComparison.Ordinal);

        var csrfToken = await GetCsrfTokenAsync(client);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/projects")
        {
            Content = JsonContent.Create(new
            {
                titel = "Cookie-sessie",
                beschrijving = "Cookie is door HttpClient hergebruikt.",
                categorie = "Test",
                datum = "2026-03-01T00:00:00"
            })
        };
        request.Headers.Add("X-CSRF-TOKEN", csrfToken);
        var protectedResponse = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, protectedResponse.StatusCode);
    }

    [Fact]
    public async Task Failed_login_is_generic_and_does_not_create_a_usable_session()
    {
        // Arrange
        using var factory = new ApiTestFactory();
        using var client = CreateClient(factory);
        var email = $"failed-login-{Guid.NewGuid():N}@example.test";
        var password = StrongPassword();
        Assert.Equal(HttpStatusCode.Created,
            (await client.PostAsJsonAsync("/api/auth/register", new { email, password })).StatusCode);

        // Act
        var login = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = StrongPassword()
        });

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
        Assert.False(login.Headers.Contains("Set-Cookie"));
        var responseBody = await login.Content.ReadAsStringAsync();
        Assert.DoesNotContain(email, responseBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(password, responseBody, StringComparison.Ordinal);
        using var write = new HttpRequestMessage(HttpMethod.Post, "/api/projects")
        {
            Content = JsonContent.Create(new
            {
                titel = "Geen sessie",
                beschrijving = "Deze wijziging mag niet slagen.",
                categorie = "Test",
                datum = "2026-03-01T00:00:00"
            })
        };
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.SendAsync(write)).StatusCode);
    }

    [Fact]
    public async Task Authenticated_write_without_csrf_token_returns_400_without_mutation()
    {
        // Arrange
        using var factory = new ApiTestFactory();
        await factory.SeedPortfolioAsync();
        using var identity = new TestIdentity(factory, "Editor");
        await identity.RegisterAndLoginAsync();

        // Act
        var response = await identity.SendWriteAsync(
            HttpMethod.Post,
            "/api/projects",
            new
            {
                titel = "CSRF geblokkeerd",
                beschrijving = "Geldige inhoud.",
                categorie = "Test",
                datum = "2026-03-01T00:00:00"
            },
            csrfToken: null);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Single((await identity.Client.GetFromJsonAsync<JsonElement[]>("/api/projects"))!);
    }

    private static HttpClient CreateClient(ApiTestFactory factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false,
            HandleCookies = true
        });

    private static string StrongPassword() =>
        $"{Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(24))}aA1!";

    private static async Task<string> GetCsrfTokenAsync(HttpClient client)
    {
        using var response = await client.GetAsync("/api/auth/csrf");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("requestToken").GetString()!;
    }
}
