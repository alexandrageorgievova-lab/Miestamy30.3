using Miestamy30._3.Models;
using Miestamy30._3.Models.Dtos;

namespace Miestamy30._3.Repositories.Interfaces;

public interface IMiestoRepository
{
    Task<int> Create(Miesto miesto);
    Task<IEnumerable<Miesto>> GetAll();
    Task<Miesto?> GetById(int id);
    Task Update(Miesto miesto);
    Task Delete(int id);

    // JOIN queries
    Task<IEnumerable<MiestoSummaryDto>> GetByKategoria(string kategoriaNazov);
    Task<IEnumerable<MiestoSummaryDto>> GetByFilter(string filterNazov);
    Task<MiestoDetailDto?> GetDetailById(int id);

    // junction table helpers
    Task AddKategoria(int miestoId, int kategoriaId, bool jeHlavna = false);
    Task AddFilter(int miestoId, int filterId);
    Task RemoveAllKategorie(int miestoId);
    Task RemoveAllFiltre(int miestoId);
}
