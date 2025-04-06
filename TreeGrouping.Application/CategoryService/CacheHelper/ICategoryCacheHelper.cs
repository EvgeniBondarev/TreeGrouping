using ConsoleApp1;
using TreeGrouping.Application.DbService.Models;

namespace TreeGrouping.Application.CategoryService.CacheHelper;

public interface ICategoryCacheHelper
{
    Task<List<CategoryModel>> GetCategoriesAsync(string cacheKey, StoredProcedureType storedProcedureType, string name = null);
    List<CategoryModel> FilterCategoriesByName(List<CategoryModel> categories, string name);
    List<CategoryModel> BuildTree(List<CategoryModel> categories);
}
