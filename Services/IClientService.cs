using ClientRecords.Models;

namespace ClientRecords.Services;

public interface IClientService
{
    IEnumerable<ClientRecord> GetByCountryCode(string countryCode);
    ClientRecord? GetById(int id);
    ClientRecord Add(ClientRecord client);
}
