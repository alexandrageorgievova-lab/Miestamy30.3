using Miestamy30._3.Models;
using Miestamy30._3.Models.Dtos;

namespace Miestamy30._3.Repositories.Interfaces;

public interface IPodujatieRepository
{
    Task<int> Create(Podujatie p);
    Task<IEnumerable<Podujatie>> GetAll();
    Task<Podujatie?> GetById(int id);
    Task Update(Podujatie p);
    Task Delete(int id);

    Task<IEnumerable<PodujatieSummaryDto>> GetByTyp(string typNazov);
    Task<IEnumerable<PodujatieSummaryDto>> GetByFilter(string filterNazov);
    Task<PodujatieDetailDto?> GetDetailById(int id);

    Task AddTyp(int podujatieId, int typId);
    Task AddFilter(int podujatieId, int filterId);
}
