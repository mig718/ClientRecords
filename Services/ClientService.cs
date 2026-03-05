using ClientRecords.Models;
using ClientRecords.Shared;

namespace ClientRecords.Services;

public class ClientService : IClientService
{
    private readonly string _csvPath;
    private readonly ReaderWriterLockSlim _rwLock = new();

    public ClientService(IConfiguration configuration)
    {
        // Allow overriding the CSV path via configuration for testing purposes
        _csvPath = configuration["ClientRecords:CsvPath"] ?? "clients.csv";

        // If no file exists, create it with empty data
        Utilities.EnsureFileExists(_csvPath);

        // Note that we are not loading all records into memory, but rather reading from the file on demand.
        // This allows independent updates to the file without needing to synchronize in-memory state.
        // I'm making this decision based on the amount of data and speed of reading a local file
        // If the data load was significant, we could consider caching with a file watcher to reload on changes, but for simplicity we'll read on demand.
    }

    public IEnumerable<ClientRecord> GetByCountryCode(string countryCode)
    {
        // Use a lightweight read lock to allow concurrent reads while ensuring we don't read while a write is in progress.
        _rwLock.EnterReadLock();
        try
        {
            return Utilities.GetRecordsFromFile(_csvPath)
                .Where(c => string.Equals(c.CountryCode, countryCode, StringComparison.OrdinalIgnoreCase)).ToList();
        }
        finally
        {
            _rwLock.ExitReadLock();
        }
    }

    public ClientRecord? GetById(int id)
    {
        _rwLock.EnterReadLock();
        try
        {
            return Utilities.GetRecordsFromFile(_csvPath)
                .FirstOrDefault(c => c.ClientId == id);
        }
        finally
        {
            _rwLock.ExitReadLock();
        }
    }

    public ClientRecord Add(ClientRecord client)
    {
        // Here we need to ensure that we assign a unique ClientId and persist the new record to the file.
        // So we're entering writer lock to ensure exclusive access while we read.
        _rwLock.EnterWriteLock();
        try
        {
            var existing = Utilities.GetRecordsFromFile(_csvPath);

            // If the client provided an ID, we should check for duplicates and throw an error if it already exists.
            if (client.ClientId != 0)
            {
                if (existing.Any(c => c.ClientId == client.ClientId))
                {
                    throw new InvalidOperationException($"A client with ID {client.ClientId} already exists.");
                }
            }
            else
            {
                // If no ID was provided, we will assign one automatically.
                client.ClientId = existing.Any() ? existing.Max(c => c.ClientId) + 1 : 1;
            }

            // Do the same for tax ID because no 2 clients should have the same tax ID.
            // This can be revised based on business rules, but for now we will enforce uniqueness of tax ID as well.
            if (existing.Any(c => c.TaxId == client.TaxId))
            {
                throw new InvalidOperationException($"A client with Tax ID {client.TaxId} already exists.");
            }

            // Normalize country code to uppercase for consistency  
            client.CountryCode = client.CountryCode.ToUpperInvariant();
            
            Utilities.AddRecordToFile(_csvPath, client);
            return client;
        }
        finally
        {
            _rwLock.ExitWriteLock();
        }
    }
}
