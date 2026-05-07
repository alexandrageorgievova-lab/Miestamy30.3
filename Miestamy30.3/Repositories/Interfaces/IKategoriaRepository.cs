using Miestamy30._3.Models;

namespace Miestamy30._3.Repositories.Interfaces;

public interface IKategoriaRepository
{
    Task<int> Create(Kategoria kategoria);
    Task<IEnumerable<Kategoria>> GetAll();
    Task<Kategoria?> GetById(int id);
    Task<Kategoria?> GetByNazov(string nazov);
    Task Update(Kategoria kategoria);
    Task Delete(int id);
}
