using AppShoping.Data.Entities;
using AppShoping.Data.Repositories;

namespace AppShoping.Data.Repositories.Extensions;

public interface IRepositoryExtensions
{
    void ExportFoodListToJsonFiles<T>(IRepository<T> repository) where T : class, IEntity;
    void ImportFoodListFromJson<T>(IRepository<T> repository) where T : class, IEntity;
    void WriteAllToConsole(IReadRepository<IEntity> allFoods);

}
