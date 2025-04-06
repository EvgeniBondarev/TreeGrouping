using TreeGrouping.Application.DbService.Models;

namespace TreeGrouping.Application.CategoryService;

public interface ICategoryService
{
    Task<List<CategoryModel>> GetVolnaCategoriesAsync(string name = null);
    Task<List<CategoryModel>> GetOzonCategoriesAsync(string name = null);
    Task<List<CategoryModel>> GetCtCategoriesAsync(string name = null);
    Task<List<CategoryModel>> GetCatTreeCategoriesAsync(string name = null);
    Task LinkCategoriesAsync(Dictionary<string, int> selectedCategories);
}
