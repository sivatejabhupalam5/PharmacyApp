using Microsoft.AspNetCore.Mvc;
using PharmacyApp.Models;
using PharmacyApp.Services;

namespace PharmacyApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController(JsonDataStore store) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<EventLogEntry>>> Get([FromQuery] int take = 100)
        => Ok(await store.GetEventsAsync(take));
}
