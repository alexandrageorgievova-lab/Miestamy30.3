using Miestamy30._3.Models;

namespace Miestamy30._3.Repositories.Interfaces;

public interface IEventFilterRepository
{
    Task<int> Create(EventFilter filter);
    Task<IEnumerable<EventFilter>> GetAll();
    Task<EventFilter?> GetById(int id);
    Task Update(EventFilter filter);
    Task Delete(int id);
}
