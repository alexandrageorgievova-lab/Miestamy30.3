using Microsoft.AspNetCore.Mvc;
using Miestamy30._3.Models;
using Miestamy30._3.Repositories.Interfaces;

namespace Miestamy30._3.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PodujatiaController(IPodujatieRepository repo) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await repo.GetAll());

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var p = await repo.GetById(id);
        return p is null ? NotFound() : Ok(p);
    }

    [HttpGet("{id:int}/detail")]
    public async Task<IActionResult> GetDetail(int id)
    {
        var d = await repo.GetDetailById(id);
        return d is null ? NotFound() : Ok(d);
    }

    // JOIN 1
    [HttpGet("by-typ/{nazov}")]
    public async Task<IActionResult> GetByTyp(string nazov) =>
        Ok(await repo.GetByTyp(nazov));

    // JOIN 2
    [HttpGet("by-filter/{nazov}")]
    public async Task<IActionResult> GetByFilter(string nazov) =>
        Ok(await repo.GetByFilter(nazov));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Podujatie p)
    {
        var id = await repo.Create(p);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] Podujatie p)
    {
        p.Id = id;
        await repo.Update(p);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await repo.Delete(id);
        return NoContent();
    }
}
