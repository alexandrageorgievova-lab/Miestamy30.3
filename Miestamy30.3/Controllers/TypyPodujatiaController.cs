using Microsoft.AspNetCore.Mvc;
using Miestamy30._3.Models;
using Miestamy30._3.Repositories.Interfaces;

namespace Miestamy30._3.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TypyPodujatiaController(ITypPodujatiaRepository repo) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await repo.GetAll());

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var t = await repo.GetById(id);
        return t is null ? NotFound() : Ok(t);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TypPodujatia typ)
    {
        var id = await repo.Create(typ);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] TypPodujatia typ)
    {
        typ.Id = id;
        await repo.Update(typ);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await repo.Delete(id);
        return NoContent();
    }
}
