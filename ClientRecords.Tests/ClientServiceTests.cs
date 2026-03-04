using ClientRecords.Models;
using ClientRecords.Services;
using Microsoft.Extensions.Configuration;

namespace ClientRecords.Tests;

public class ClientServiceTests : IDisposable
{
    private readonly string _csvPath;
    private readonly ClientService _service;

    public ClientServiceTests()
    {
        _csvPath = Path.Combine(Path.GetTempPath(), $"clients_{Guid.NewGuid():N}.csv");
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ClientRecords:CsvPath"] = _csvPath
            })
            .Build();

        _service = new ClientService(config);
    }

    public void Dispose()
    {
        if (File.Exists(_csvPath))
            File.Delete(_csvPath);
    }

    [Fact]
    public void GetAll_ReturnsEmpty_WhenNoRecords()
    {
        var result = _service.GetAll();
        Assert.Empty(result);
    }

    [Fact]
    public void Add_AssignsClientId_AndPersists()
    {
        var client = new ClientRecord { Name = "Alice", TaxId = "TX001", CountryCode = "US" };

        var created = _service.Add(client);

        Assert.Equal(1, created.ClientId);
        Assert.Single(_service.GetAll());
    }

    [Fact]
    public void Add_IncrementsClientId_ForMultipleRecords()
    {
        _service.Add(new ClientRecord { Name = "Alice", TaxId = "TX001", CountryCode = "US" });
        var second = _service.Add(new ClientRecord { Name = "Bob", TaxId = "TX002", CountryCode = "GB" });

        Assert.Equal(2, second.ClientId);
        Assert.Equal(2, _service.GetAll().Count());
    }

    [Fact]
    public void GetById_ReturnsCorrectRecord()
    {
        _service.Add(new ClientRecord { Name = "Alice", TaxId = "TX001", CountryCode = "US" });
        _service.Add(new ClientRecord { Name = "Bob", TaxId = "TX002", CountryCode = "GB" });

        var result = _service.GetById(2);

        Assert.NotNull(result);
        Assert.Equal("Bob", result!.Name);
    }

    [Fact]
    public void GetById_ReturnsNull_WhenNotFound()
    {
        var result = _service.GetById(99);
        Assert.Null(result);
    }

    [Fact]
    public void Add_PersistsToCsv_WithCorrectFields()
    {
        var client = new ClientRecord { Name = "Alice", TaxId = "TX001", CountryCode = "US" };
        _service.Add(client);

        var lines = File.ReadAllLines(_csvPath);
        Assert.Equal(2, lines.Length); // header + 1 record
        Assert.Equal("client_id,name,tax_id,country_code", lines[0]);
        Assert.Equal("1,Alice,TX001,US", lines[1]);
    }
}
