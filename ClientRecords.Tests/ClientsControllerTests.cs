using System.Net;
using System.Net.Http.Json;
using ClientRecords.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ClientRecords.Tests;

public class ClientsControllerTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly string _csvPath;

    public ClientsControllerTests(WebApplicationFactory<Program> factory)
    {
        _csvPath = Path.Combine(Path.GetTempPath(), $"clients_{Guid.NewGuid():N}.csv");

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ClientRecords:CsvPath"] = _csvPath
                });
            });
        });

        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        if (File.Exists(_csvPath))
            File.Delete(_csvPath);
    }

    [Fact]
    public async Task GetByCountryCode_ReturnsEmptyArray_Initially()
    {
        var response = await _client.GetAsync("/api/clients/US");
        response.EnsureSuccessStatusCode();

        var records = await response.Content.ReadFromJsonAsync<ClientRecord[]>();
        Assert.NotNull(records);
        Assert.Empty(records);
    }

    [Fact]
    public async Task Post_CreatesRecord_AndReturnsCreated()
    {
        var newClient = new { Name = "Alice", TaxId = "TX001", CountryCode = "US" };

        var response = await _client.PostAsJsonAsync("/api/clients", newClient);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<ClientRecord>();
        Assert.NotNull(created);
        Assert.Equal(1, created!.ClientId);
        Assert.Equal("Alice", created.Name);
    }

    [Fact]
    public async Task GetById_ReturnsRecord_WhenExists()
    {
        var newClient = new { Name = "Bob", TaxId = "TX002", CountryCode = "GB" };
        var postResponse = await _client.PostAsJsonAsync("/api/clients", newClient);
        var created = await postResponse.Content.ReadFromJsonAsync<ClientRecord>();

        var response = await _client.GetAsync($"/api/clients/{created!.ClientId}");
        response.EnsureSuccessStatusCode();

        var record = await response.Content.ReadFromJsonAsync<ClientRecord>();
        Assert.NotNull(record);
        Assert.Equal("Bob", record!.Name);
    }

    [Fact]
    public async Task GetById_Returns404_WhenNotFound()
    {
        var response = await _client.GetAsync("/api/clients/999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetByCountryCode_ReturnsOnlyMatchingRecords()
    {
        await _client.PostAsJsonAsync("/api/clients", new { Name = "Alice", TaxId = "TX001", CountryCode = "US" });
        await _client.PostAsJsonAsync("/api/clients", new { Name = "Bob", TaxId = "TX002", CountryCode = "GB" });

        var response = await _client.GetAsync("/api/clients/US");
        response.EnsureSuccessStatusCode();

        var records = await response.Content.ReadFromJsonAsync<ClientRecord[]>();
        Assert.NotNull(records);
        Assert.Single(records!);
        Assert.Equal("US", records[0].CountryCode);
    }

    [Fact]
    public async Task Post_Returns422_WhenTaxIdIsDuplicate()
    {
        await _client.PostAsJsonAsync("/api/clients", new { Name = "Alice", TaxId = "TX001", CountryCode = "US" });

        var response = await _client.PostAsJsonAsync("/api/clients", new { Name = "Bob", TaxId = "TX001", CountryCode = "GB" });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(422, problem!.Status);
        Assert.Equal("A client with Tax ID TX001 already exists.", problem.Detail);
    }

    [Fact]
    public async Task Post_Returns422_WhenClientIdIsDuplicate()
    {
        await _client.PostAsJsonAsync("/api/clients", new { ClientId = 7, Name = "Alice", TaxId = "TX001", CountryCode = "US" });

        var response = await _client.PostAsJsonAsync("/api/clients", new { ClientId = 7, Name = "Bob", TaxId = "TX002", CountryCode = "GB" });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(422, problem!.Status);
        Assert.Equal("A client with ID 7 already exists.", problem.Detail);
    }
}
