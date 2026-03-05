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

    [HttpGet("{countryCode:alpha}")]
    public ActionResult<IEnumerable<ClientRecord>> GetByCountryCode(string countryCode)
    {
        return Ok(_clientService.GetByCountryCode(countryCode));
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
        try
        {
            var created = _clientService.Add(client);
            return CreatedAtAction(nameof(GetById), new { id = created.ClientId }, created);
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new ProblemDetails
            {
                Status = StatusCodes.Status422UnprocessableEntity,
                Title = "Duplicate client data",
                Detail = ex.Message
            });
        }
    }
}
