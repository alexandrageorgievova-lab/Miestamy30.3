using Microsoft.AspNetCore.Mvc;
using Miestamy30._3.Models;
using Miestamy30._3.Repositories.Interfaces;

namespace Miestamy30._3.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventFiltersController(IEventFilterRepository repo) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await repo.GetAll());

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var f = await repo.GetById(id);
        return f is null ? NotFound() : Ok(f);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] EventFilter filter)
    {
        var id = await repo.Create(filter);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] EventFilter filter)
    {
        filter.Id = id;
        await repo.Update(filter);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await repo.Delete(id);
        return NoContent();
    }
}
