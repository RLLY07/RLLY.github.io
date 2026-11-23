using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Portfolio.Api.Tests;

public class WritePolicyTests
{
    public static IEnumerable<object[]> WriteScenarios
    {
        get
        {
            foreach (var route in new[]
            {
                new Route("POST", "/api/projects", "Projects"),
                new Route("PUT", "/api/projects/1", "Projects"),
                new Route("DELETE", "/api/projects/1", "Projects"),
                new Route("POST", "/api/blogposts", "Blogposts"),
                new Route("PUT", "/api/blogposts/1", "Blogposts"),
                new Route("DELETE", "/api/blogposts/1", "Blogposts")
            })
            {
                yield return [route.Method, route.Path, route.Resource, "Anonymous", 401];
                yield return [route.Method, route.Path, route.Resource, "Gast", 403];
                yield return [route.Method, route.Path, route.Resource, "Editor", route.Method == "POST" ? 201 : 204];
                yield return [route.Method, route.Path, route.Resource, "Admin", route.Method == "POST" ? 201 : 204];
            }
        }
    }

    [Theory]
    [MemberData(nameof(WriteScenarios))]
    public async Task Write_policy_matrix_has_expected_status_and_mutation(
        string methodName,
        string path,
        string resource,
        string role,
        int expectedStatus)
    {
        // Arrange
        using var factory = new ApiTestFactory();
        await factory.SeedPortfolioAsync();
        using var identity = role == "Anonymous" ? null : new TestIdentity(factory, role);
        using var anonymousClient = identity is null ? CreateClient(factory) : null;
        var client = identity?.Client ?? anonymousClient!;
        if (identity is not null)
        {
            await identity.RegisterAndLoginAsync();
        }

        var csrfToken = identity is null ? null : await identity.GetCsrfTokenAsync();
        var collectionPath = $"/api/{resource.ToLowerInvariant()}";
        var beforeList = await client.GetFromJsonAsync<JsonElement[]>(collectionPath);
        Assert.NotNull(beforeList);
        var beforeDetail = await client.GetFromJsonAsync<JsonElement>($"{collectionPath}/1");

        // Act
        var response = await SendRequestAsync(
            client,
            HttpMethod.Parse(methodName),
            path,
            ValidBody(resource, methodName == "POST" ? "Nieuw item" : "Gewijzigd item"),
            csrfToken);

        // Assert
        Assert.Equal(expectedStatus, (int)response.StatusCode);
        if (expectedStatus is 401 or 403)
        {
            var afterList = await client.GetFromJsonAsync<JsonElement[]>(collectionPath);
            var afterDetail = await client.GetFromJsonAsync<JsonElement>($"{collectionPath}/1");
            Assert.Equal(beforeList.Length, afterList!.Length);
            Assert.Equal(beforeDetail.GetRawText(), afterDetail.GetRawText());
            return;
        }

        if (methodName == "POST")
        {
            Assert.NotNull(response.Headers.Location);
            using var created = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var id = created.RootElement.GetProperty("id").GetInt32();
            Assert.True(id > 0);
            Assert.EndsWith($"/{id}", response.Headers.Location!.ToString(), StringComparison.Ordinal);
            using var persisted = await client.GetAsync(response.Headers.Location);
            Assert.Equal(HttpStatusCode.OK, persisted.StatusCode);
            using var saved = JsonDocument.Parse(await persisted.Content.ReadAsStringAsync());
            AssertExpectedDto(saved.RootElement, resource, id, "Nieuw item");
            return;
        }

        if (methodName == "PUT")
        {
            var updated = await client.GetFromJsonAsync<JsonElement>($"{collectionPath}/1");
            AssertExpectedDto(updated, resource, 1, "Gewijzigd item");
            return;
        }

        var deleted = await client.GetAsync($"{collectionPath}/1");
        Assert.Equal(HttpStatusCode.NotFound, deleted.StatusCode);
    }

    public static IEnumerable<object[]> ResourceRoutes
    {
        get
        {
            yield return ["/api/projects", "Projects"];
            yield return ["/api/blogposts", "Blogposts"];
        }
    }

    [Theory]
    [MemberData(nameof(ResourceRoutes))]
    public async Task Public_get_routes_return_concrete_records_and_unknown_id_is_404(
        string path,
        string resource)
    {
        // Arrange
        using var factory = new ApiTestFactory();
        await factory.SeedPortfolioAsync();
        using var client = CreateClient(factory);

        // Act
        var listResponse = await client.GetAsync(path);
        var detailResponse = await client.GetAsync($"{path}/1");
        var missingResponse = await client.GetAsync($"{path}/2147483647");

        // Assert
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missingResponse.StatusCode);
        using var list = JsonDocument.Parse(await listResponse.Content.ReadAsStringAsync());
        using var detail = JsonDocument.Parse(await detailResponse.Content.ReadAsStringAsync());
        Assert.Equal(1, list.RootElement.GetArrayLength());
        AssertExpectedDto(list.RootElement[0], resource, 1,
            resource == "Projects" ? "Bestaand project" : "Bestaande blogpost");
        AssertExpectedDto(detail.RootElement, resource, 1,
            resource == "Projects" ? "Bestaand project" : "Bestaande blogpost");
    }

    public static IEnumerable<object[]> InvalidWriteScenarios
    {
        get
        {
            yield return ["POST", "/api/projects", "Projects"];
            yield return ["PUT", "/api/projects/1", "Projects"];
            yield return ["POST", "/api/blogposts", "Blogposts"];
            yield return ["PUT", "/api/blogposts/1", "Blogposts"];
        }
    }

    [Theory]
    [MemberData(nameof(InvalidWriteScenarios))]
    public async Task Invalid_write_body_returns_400_without_mutation(string methodName, string path, string resource)
    {
        // Arrange
        using var factory = new ApiTestFactory();
        await factory.SeedPortfolioAsync();
        using var identity = new TestIdentity(factory, "Editor");
        await identity.RegisterAndLoginAsync();
        var csrfToken = await identity.GetCsrfTokenAsync();
        var collectionPath = $"/api/{resource.ToLowerInvariant()}";

        // Act
        var response = await SendRequestAsync(
            identity.Client,
            HttpMethod.Parse(methodName),
            path,
            new { titel = "", beschrijving = "valid", categorie = "Test", inhoud = "valid", datum = "2026-02-01" },
            csrfToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var records = await identity.Client.GetFromJsonAsync<JsonElement[]>(collectionPath);
        Assert.Single(records!);
        var existing = await identity.Client.GetFromJsonAsync<JsonElement>($"{collectionPath}/1");
        var expectedTitle = resource == "Projects" ? "Bestaand project" : "Bestaande blogpost";
        AssertExpectedDto(existing, resource, 1, expectedTitle);
    }

    public static IEnumerable<object[]> MissingWriteScenarios
    {
        get
        {
            foreach (var resource in new[] { "projects", "blogposts" })
            {
                yield return ["PUT", $"/api/{resource}/2147483647", resource];
                yield return ["DELETE", $"/api/{resource}/2147483647", resource];
            }
        }
    }

    [Theory]
    [MemberData(nameof(MissingWriteScenarios))]
    public async Task Write_for_unknown_id_returns_404_without_mutation(
        string methodName,
        string path,
        string resource)
    {
        // Arrange
        using var factory = new ApiTestFactory();
        await factory.SeedPortfolioAsync();
        using var identity = new TestIdentity(factory, "Admin");
        await identity.RegisterAndLoginAsync();
        var csrfToken = await identity.GetCsrfTokenAsync();

        // Act
        var response = await SendRequestAsync(
            identity.Client,
            HttpMethod.Parse(methodName),
            path,
            methodName == "PUT" ? ValidBody(resource, "Onbereikbaar") : null,
            csrfToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var existing = await identity.Client.GetFromJsonAsync<JsonElement>($"/api/{resource}/1");
        var expectedTitle = resource == "projects" ? "Bestaand project" : "Bestaande blogpost";
        AssertExpectedDto(existing, resource, 1, expectedTitle);
    }

    private static HttpClient CreateClient(ApiTestFactory factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false,
            HandleCookies = true
        });

    private static object ValidBody(string resource, string title) =>
        resource.Equals("Projects", StringComparison.OrdinalIgnoreCase)
            ? new
            {
                titel = title,
                beschrijving = "Geldige testbeschrijving.",
                categorie = "Integratietest",
                gitHubUrl = "https://example.test/nieuw",
                datum = "2026-02-04T05:06:07"
            }
            : new
            {
                titel = title,
                inhoud = "Geldige testinhoud.",
                publicatiedatum = "2026-02-04T05:06:07"
            };

    private static void AssertExpectedDto(JsonElement dto, string resource, int id, string title)
    {
        Assert.Equal(id, dto.GetProperty("id").GetInt32());
        Assert.Equal(title, dto.GetProperty("titel").GetString());
        if (resource.Equals("Projects", StringComparison.OrdinalIgnoreCase))
        {
            var isExisting = title == "Bestaand project";
            Assert.Equal(isExisting ? "Project voor integratietests." : "Geldige testbeschrijving.",
                dto.GetProperty("beschrijving").GetString());
            Assert.Equal(isExisting ? "Test" : "Integratietest", dto.GetProperty("categorie").GetString());
            Assert.Equal(isExisting ? "https://example.test/project" : "https://example.test/nieuw",
                dto.GetProperty("gitHubUrl").GetString());
            Assert.Equal(isExisting ? "2026-01-02T00:00:00" : "2026-02-04T05:06:07",
                dto.GetProperty("datum").GetString());
            return;
        }

        var existingBlogpost = title == "Bestaande blogpost";
        Assert.Equal(existingBlogpost ? "Blogpost voor integratietests." : "Geldige testinhoud.",
            dto.GetProperty("inhoud").GetString());
        Assert.Equal(
            existingBlogpost ? "2026-01-03T00:00:00" : "2026-02-04T05:06:07",
            dto.GetProperty("publicatiedatum").GetString());
    }

    private static async Task<HttpResponseMessage> SendRequestAsync(
        HttpClient client,
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

        return await client.SendAsync(request);
    }

    private sealed record Route(string Method, string Path, string Resource);
}
