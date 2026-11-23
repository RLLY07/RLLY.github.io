using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Portfolio.Api.Services;

namespace Portfolio.Api.Tests;

public class SecurityStampTests
{
    [Fact]
    public async Task Role_assignment_is_idempotent_and_reports_unknown_role_or_account()
    {
        // Arrange
        using var factory = new ApiTestFactory();
        using var identity = new TestIdentity(factory);
        await identity.RegisterAndLoginAsync();
        await using var scope = factory.Services.CreateAsyncScope();
        var roleAdministration = scope.ServiceProvider.GetRequiredService<RoleAdministrationService>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var user = await userManager.FindByEmailAsync(identity.Email);
        Assert.NotNull(user);
        var initialStamp = await userManager.GetSecurityStampAsync(user);

        // Act
        await roleAdministration.SetRoleAsync(identity.Email, "Gast");
        var repeatedStamp = await userManager.GetSecurityStampAsync(user);
        var missingRole = await Assert.ThrowsAsync<InvalidOperationException>(
            () => roleAdministration.SetRoleAsync(identity.Email, "Unknown"));
        var missingUser = await Assert.ThrowsAsync<InvalidOperationException>(
            () => roleAdministration.SetRoleAsync("missing@example.test", "Editor"));

        // Assert
        Assert.Equal(initialStamp, repeatedStamp);
        Assert.Equal("The requested role does not exist.", missingRole.Message);
        Assert.Equal("No account exists for the requested email.", missingUser.Message);
        Assert.Equal(new[] { "Gast" }, await userManager.GetRolesAsync(user));
    }

    [Fact]
    public async Task Promotion_and_revocation_invalidate_old_cookies_and_require_fresh_login()
    {
        // Arrange
        using var factory = new ApiTestFactory();
        await factory.SeedPortfolioAsync();
        using var guest = new TestIdentity(factory);
        await guest.RegisterAndLoginAsync();
        var oldGuestToken = await guest.GetCsrfTokenAsync();
        await using var roleScope = factory.Services.CreateAsyncScope();
        var roleAdministration = roleScope.ServiceProvider.GetRequiredService<RoleAdministrationService>();

        // Act: promote while the Guest session remains authenticated.
        await roleAdministration.SetRoleAsync(guest.Email, "Editor");
        using var staleGuestWrite = await guest.SendWriteAsync(
            HttpMethod.Post,
            "/api/projects",
            ProjectBody("Stale Guest-cookie"),
            oldGuestToken);

        // Assert: the old cookie is rejected rather than refreshed into Editor.
        Assert.Equal(HttpStatusCode.Unauthorized, staleGuestWrite.StatusCode);

        // Arrange: sign in again after promotion in a distinct cookie container.
        using var editorClient = CreateClient(factory);
        var editorLogin = await editorClient.PostAsJsonAsync("/api/auth/login", new
        {
            email = guest.Email,
            password = guest.Password
        });
        Assert.Equal(HttpStatusCode.OK, editorLogin.StatusCode);
        var editorToken = await GetCsrfTokenAsync(editorClient);

        // Act: the fresh Editor session can write, then the role is revoked.
        using var promotedWrite = await SendWriteAsync(
            editorClient,
            HttpMethod.Post,
            "/api/projects",
            ProjectBody("Fresh Editor-cookie"),
            editorToken);
        Assert.Equal(HttpStatusCode.Created, promotedWrite.StatusCode);
        await roleAdministration.SetRoleAsync(guest.Email, "Gast");
        using var staleEditorWrite = await SendWriteAsync(
            editorClient,
            HttpMethod.Post,
            "/api/projects",
            ProjectBody("Stale Editor-cookie"),
            editorToken);

        // Assert: the old Editor cookie is rejected and a fresh Guest cannot write.
        Assert.Equal(HttpStatusCode.Unauthorized, staleEditorWrite.StatusCode);
        using var freshGuestClient = CreateClient(factory);
        var guestLogin = await freshGuestClient.PostAsJsonAsync("/api/auth/login", new
        {
            email = guest.Email,
            password = guest.Password
        });
        Assert.Equal(HttpStatusCode.OK, guestLogin.StatusCode);
        var freshGuestToken = await GetCsrfTokenAsync(freshGuestClient);
        using var deniedWrite = await SendWriteAsync(
            freshGuestClient,
            HttpMethod.Post,
            "/api/projects",
            ProjectBody("Fresh Guest-cookie"),
            freshGuestToken);
        Assert.Equal(HttpStatusCode.Forbidden, deniedWrite.StatusCode);
    }

    private static HttpClient CreateClient(ApiTestFactory factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false,
            HandleCookies = true
        });

    private static object ProjectBody(string title) => new
    {
        titel = title,
        beschrijving = "Beveiligingsstamptest.",
        categorie = "Test",
        datum = "2026-03-04T00:00:00"
    };

    private static async Task<string> GetCsrfTokenAsync(HttpClient client)
    {
        using var response = await client.GetAsync("/api/auth/csrf");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("requestToken").GetString()!;
    }

    private static async Task<HttpResponseMessage> SendWriteAsync(
        HttpClient client,
        HttpMethod method,
        string path,
        object body,
        string token)
    {
        using var request = new HttpRequestMessage(method, path)
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.Add("X-CSRF-TOKEN", token);
        return await client.SendAsync(request);
    }
}
