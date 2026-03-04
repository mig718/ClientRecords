using ClientRecords.Models;
using ClientRecords.Services;
using Microsoft.AspNetCore.Mvc;

namespace ClientRecords.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClientsController : ControllerBase
{
    private readonly IClientService _clientService;

    public ClientsController(IClientService clientService)
    {
        _clientService = clientService;
    }

    [HttpGet]
    public ActionResult<IEnumerable<ClientRecord>> GetAll()
    {
        return Ok(_clientService.GetAll());
    }

    [HttpGet("{id:int}")]
    public ActionResult<ClientRecord> GetById(int id)
    {
        var client = _clientService.GetById(id);
        if (client == null)
            return NotFound();

        return Ok(client);
    }

    [HttpPost]
    public ActionResult<ClientRecord> Add([FromBody] ClientRecord client)
    {
        var created = _clientService.Add(client);
        return CreatedAtAction(nameof(GetById), new { id = created.ClientId }, created);
    }
}
