using Miestamy30._3.Models;

namespace Miestamy30._3.Repositories.Interfaces;

public interface ITypPodujatiaRepository
{
    Task<int> Create(TypPodujatia typ);
    Task<IEnumerable<TypPodujatia>> GetAll();
    Task<TypPodujatia?> GetById(int id);
    Task Update(TypPodujatia typ);
    Task Delete(int id);
    Task<IEnumerable<string>> GetActiveTypNames();
}
