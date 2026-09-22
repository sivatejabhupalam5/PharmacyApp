using Microsoft.AspNetCore.Mvc;
using PharmacyApp.Models;
using PharmacyApp.Services;

namespace PharmacyApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MedicinesController(JsonDataStore store) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Medicine>>> Get([FromQuery] string? search)
        => Ok(await store.GetMedicinesAsync(search));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Medicine>> Get(int id)
    {
        var medicine = await store.GetMedicineAsync(id);
        return medicine is null ? NotFound() : Ok(medicine);
    }

    [HttpPost]
    public async Task<ActionResult<Medicine>> Post([FromBody] Medicine medicine)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        medicine.Name = medicine.Name.Trim();
        medicine.Brand = medicine.Brand.Trim();
        medicine.Price = Math.Round(medicine.Price, 2);

        var created = await store.AddMedicineAsync(medicine);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }
}
