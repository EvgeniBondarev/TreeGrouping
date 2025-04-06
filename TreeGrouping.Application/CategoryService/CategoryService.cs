using ConsoleApp1;
using TreeGrouping.Application.DbService;
using TreeGrouping.Application.DbService.Models;

namespace TreeGrouping.Application.CategoryService.CacheHelper;

public class CategoryService : ICategoryService
{
    private readonly IDatabaseService _dbService;
    private readonly ICategoryCacheHelper _cacheHelper;

    public CategoryService(IDatabaseService dbService, ICategoryCacheHelper cacheHelper)
    {
        _dbService = dbService;
        _cacheHelper = cacheHelper;
    }

    public Task<List<CategoryModel>> GetVolnaCategoriesAsync(string name = null) =>
        _cacheHelper.GetCategoriesAsync("volna_categories", StoredProcedureType.GetVolnaCategories, name);

    public Task<List<CategoryModel>> GetOzonCategoriesAsync(string name = null) =>
        _cacheHelper.GetCategoriesAsync("ozon_categories", StoredProcedureType.GetOzonCategories, name);

    public async Task<List<CategoryModel>> GetCtCategoriesAsync(string name = null)
    {
        var baseCategories = await _cacheHelper.GetCategoriesAsync("ct_categories", StoredProcedureType.GetCtCategories);
        var links = await _dbService.GetAllCategoryLinksAsync();
        var linkModels = await ConvertCategoryLinksToModelsAsync(links.ToList());

        baseCategories.AddRange(linkModels);
        return _cacheHelper.FilterCategoriesByName(baseCategories, name);
    }

    public Task<List<CategoryModel>> GetCatTreeCategoriesAsync(string name = null) =>
        _cacheHelper.GetCategoriesAsync("cat_tree_categories", StoredProcedureType.GetCatTreeCategories, name);

    public async Task LinkCategoriesAsync(Dictionary<string, int> selectedCategories)
    {
        if (selectedCategories == null || selectedCategories.Count <= 1 || !selectedCategories.ContainsKey("CT"))
            throw new ArgumentException("Необходимо выбрать элемент с ключом 'CT' и хотя бы одну дополнительную категорию.");

        var ctValue = selectedCategories["CT"];

        var categoryPairs = selectedCategories
            .Where(kvp => kvp.Key != "CT")
            .Select(kvp => new { CtCategoryId = ctValue, LinkCategoryId = kvp.Value, LinkTypeName = kvp.Key });

        foreach (var pair in categoryPairs)
        {
            await _dbService.ExecuteStoredProcedureAsync(
                StoredProcedureType.AddCategoryLink,
                (pair.CtCategoryId, pair.LinkCategoryId, pair.LinkTypeName)
            );
        }
    }

    private async Task<List<CategoryModel>> ConvertCategoryLinksToModelsAsync(List<CategoryLinkModel> links)
    {
        var tasks = links.Select(async link =>
        {
            if (!Enum.TryParse<CategoryLinkType>(link.LinkTypeName, true, out var linkType))
                return Enumerable.Empty<CategoryModel>();

            var linkTree = await _dbService.ExecuteStoredProcedureAsync(linkType.GetProcedureType(), link.LinkCategoryId);
            var tree = _cacheHelper.BuildTree(linkTree.ToList());

            if (tree.Any())
            {
                tree.First().ParentId = link.CtCategoryId;
                tree.First().ParentName = link.LinkTypeName;
            }

            return tree;
        });

        var result = await Task.WhenAll(tasks);
        return result.SelectMany(x => x).ToList();
    }
}
