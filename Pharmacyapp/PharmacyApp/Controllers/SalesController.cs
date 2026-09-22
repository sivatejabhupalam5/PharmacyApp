using Microsoft.AspNetCore.Mvc;
using PharmacyApp.Models;
using PharmacyApp.Services;

namespace PharmacyApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SalesController(JsonDataStore store) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Sale>>> Get() => Ok(await store.GetSalesAsync());

    [HttpPost]
    public async Task<ActionResult<Sale>> Post([FromBody] SaleRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var (sale, error) = await store.RecordSaleAsync(request);
        return sale is null ? BadRequest(new { message = error }) : Ok(sale);
    }
}
