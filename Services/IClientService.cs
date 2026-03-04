using ClientRecords.Models;

namespace ClientRecords.Services;

public interface IClientService
{
    IEnumerable<ClientRecord> GetAll();
    ClientRecord? GetById(int id);
    ClientRecord Add(ClientRecord client);
}
