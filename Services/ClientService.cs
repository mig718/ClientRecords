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

            // Assign a new ClientId based on the max existing ID + 1, or start at 1 if no records exist.
            // This auto-assignment ensures that clients don't need to provide an ID and that we maintain a consistent sequence.
            client.ClientId = existing.Count > 0 ? existing.Max(c => c.ClientId) + 1 : 1;
            client.CountryCode = client.CountryCode.ToUpperInvariant(); // Normalize country code to uppercase for consistency  
            Utilities.AddRecordToFile(_csvPath, client);
            return client;
        }
        finally
        {
            _rwLock.ExitWriteLock();
        }
    }
}
