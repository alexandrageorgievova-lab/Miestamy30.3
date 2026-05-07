using Miestamy30._3.Models;

namespace Miestamy30._3.Repositories.Interfaces;

public interface IFilterRepository
{
    Task<int> Create(Filter filter);
    Task<IEnumerable<Filter>> GetAll();
    Task<Filter?> GetById(int id);
    Task<IEnumerable<Filter>> GetByKategoriaId(int kategoriaId);
    Task Update(Filter filter);
    Task Delete(int id);
}
