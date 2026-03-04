using ClientRecords.Models;

namespace ClientRecords.Services;

public class ClientService : IClientService
{
    private readonly string _csvPath;
    private readonly object _lock = new();

    public ClientService(IConfiguration configuration)
    {
        _csvPath = configuration["ClientRecords:CsvPath"] ?? "clients.csv";
        EnsureFileExists();
    }

    public IEnumerable<ClientRecord> GetAll()
    {
        lock (_lock)
        {
            return ReadAll();
        }
    }

    public ClientRecord? GetById(int id)
    {
        lock (_lock)
        {
            return ReadAll().FirstOrDefault(c => c.ClientId == id);
        }
    }

    public ClientRecord Add(ClientRecord client)
    {
        lock (_lock)
        {
            var existing = ReadAll().ToList();
            client.ClientId = existing.Count > 0 ? existing.Max(c => c.ClientId) + 1 : 1;
            AppendLine(ToCsvLine(client));
            return client;
        }
    }

    private void EnsureFileExists()
    {
        if (!File.Exists(_csvPath))
        {
            File.WriteAllText(_csvPath, "client_id,name,tax_id,country_code\n");
        }
    }

    private List<ClientRecord> ReadAll()
    {
        var records = new List<ClientRecord>();
        var lines = File.ReadAllLines(_csvPath);
        // Skip header row
        foreach (var line in lines.Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var record = ParseLine(line);
            if (record != null)
                records.Add(record);
        }
        return records;
    }

    private static ClientRecord? ParseLine(string line)
    {
        var parts = SplitCsvLine(line);
        if (parts.Length < 4)
            return null;

        if (!int.TryParse(parts[0].Trim(), out var id))
            return null;

        return new ClientRecord
        {
            ClientId = id,
            Name = Unescape(parts[1]),
            TaxId = Unescape(parts[2]),
            CountryCode = Unescape(parts[3])
        };
    }

    private static string ToCsvLine(ClientRecord client)
    {
        return $"{client.ClientId},{Escape(client.Name)},{Escape(client.TaxId)},{Escape(client.CountryCode)}";
    }

    private static string Escape(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
        return value;
    }

    private static string Unescape(string value)
    {
        value = value.Trim();
        if (value.StartsWith('"') && value.EndsWith('"') && value.Length >= 2)
        {
            value = value[1..^1].Replace("\"\"", "\"");
        }
        return value;
    }

    private static string[] SplitCsvLine(string line)
    {
        var fields = new List<string>();
        var current = new System.Text.StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (inQuotes)
            {
                if (c == '"' && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else if (c == '"')
                {
                    inQuotes = false;
                }
                else
                {
                    current.Append(c);
                }
            }
            else
            {
                if (c == '"')
                {
                    inQuotes = true;
                }
                else if (c == ',')
                {
                    fields.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }
        }
        fields.Add(current.ToString());
        return fields.ToArray();
    }

    private void AppendLine(string line)
    {
        File.AppendAllText(_csvPath, line + "\n");
    }
}
