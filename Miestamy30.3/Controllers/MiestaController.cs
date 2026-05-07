using Microsoft.AspNetCore.Mvc;
using Miestamy30._3.Models;
using Miestamy30._3.Repositories.Interfaces;

namespace Miestamy30._3.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MiestaController(IMiestoRepository repo) : ControllerBase
{
    // ── Basic CRUD ───────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await repo.GetAll());

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var m = await repo.GetById(id);
        return m is null ? NotFound() : Ok(m);
    }

    // JOIN query 3 – detail so všetkými kategóriami a filtrami
    [HttpGet("{id:int}/detail")]
    public async Task<IActionResult> GetDetail(int id)
    {
        var detail = await repo.GetDetailById(id);
        return detail is null ? NotFound() : Ok(detail);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Miesto miesto)
    {
        var id = await repo.Create(miesto);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] Miesto miesto)
    {
        miesto.Id = id;
        await repo.Update(miesto);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await repo.Delete(id);
        return NoContent();
    }

    // ── JOIN query 1 – miesta podľa názvu kategórie ──────────────────────────────
    [HttpGet("by-kategoria/{nazov}")]
    public async Task<IActionResult> GetByKategoria(string nazov) =>
        Ok(await repo.GetByKategoria(nazov));

    // ── JOIN query 2 – miesta podľa filtra ───────────────────────────────────────
    [HttpGet("by-filter/{nazov}")]
    public async Task<IActionResult> GetByFilter(string nazov) =>
        Ok(await repo.GetByFilter(nazov));
}
