using ClientRecords.Models;

namespace ClientRecords.Shared;

public static class Utilities
{
    public const string CsvHeader = "client_id,name,tax_id,country_code";

	public static ClientRecord? ToClientRecord(this string line)
	{
		var tokens = line.Split(',');
		if (tokens.Length < 4)
			return null;

		if (!int.TryParse(tokens[0].Trim(), out var id))
			return null;

		return new ClientRecord
		{
			ClientId = id,
			Name = tokens[1],
			TaxId = tokens[2],
			CountryCode = tokens[3]
		};
	}

	public static string ToCsvLine(this ClientRecord client)
	{
		return $"{client.ClientId},{client.Name},{client.TaxId},{client.CountryCode}";
	}

    public static void EnsureFileExists(string path)
    {
        if (!File.Exists(path))
        {
            File.WriteAllText(path, CsvHeader + "\n");
        }
    }

    public static List<ClientRecord> GetRecordsFromFile(string path)
    {
        var records = new List<ClientRecord>();
        var lines = File.ReadAllLines(path);
        // Skip header row
        foreach (var line in lines.Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var record = line.ToClientRecord();
            if (record != null)
                records.Add(record);
        }
        return records;
    }

    public static void AddRecordToFile(string path, ClientRecord client)
    {
        var line = client.ToCsvLine();
        File.AppendAllText(path, line + "\n");
    }
}
