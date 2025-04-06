using ConsoleApp1;
using Microsoft.Extensions.Caching.Memory;
using TreeGrouping.Application.DbService;
using TreeGrouping.Application.DbService.Models;

namespace TreeGrouping.Application.CategoryService.CacheHelper;

public class CategoryCacheHelper : ICategoryCacheHelper
{
    private readonly IDatabaseService _dbService;
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(30);

    public CategoryCacheHelper(IDatabaseService dbService, IMemoryCache cache)
    {
        _dbService = dbService;
        _cache = cache;
    }

    public async Task<List<CategoryModel>> GetCategoriesAsync(string cacheKey, StoredProcedureType storedProcedureType, string name = null)
    {
        if (!_cache.TryGetValue(cacheKey, out List<CategoryModel> categories))
        {
            categories = (await _dbService.ExecuteStoredProcedureAsync(storedProcedureType)).ToList();
            _cache.Set(cacheKey, categories, _cacheDuration);
        }

        return FilterCategoriesByName(categories, name);
    }

    public List<CategoryModel> FilterCategoriesByName(List<CategoryModel> categories, string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            categories.ForEach(c => c.IsFiltred = false);
            return categories;
        }

        var filtered = categories
            .Where(c => c.Name.Contains(name, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var parentIds = new HashSet<int?>(filtered.Select(c => c.ParentId));
        var allRelevant = new HashSet<CategoryModel>(filtered);

        while (parentIds.Count > 0)
        {
            var newParents = categories
                .Where(c => parentIds.Contains(c.Id) && !allRelevant.Contains(c))
                .ToList();

            if (!newParents.Any()) break;

            foreach (var parent in newParents)
                allRelevant.Add(parent);

            parentIds = new HashSet<int?>(newParents.Select(c => c.ParentId));
        }

        foreach (var category in allRelevant)
            category.IsFiltred = true;

        return allRelevant.ToList();
    }

    public List<CategoryModel> BuildTree(List<CategoryModel> categories)
    {
        var lookup = categories.ToLookup(c => c.ParentId);

        List<CategoryModel> BuildBranch(int? parentId) =>
            lookup[parentId]
                .Select(c => new CategoryModel
                {
                    Id = c.Id,
                    ParentId = c.ParentId,
                    Name = c.Name,
                    ParentName = c.ParentName,
                    Children = c.Children.Any() ? c.Children : BuildBranch(c.Id),
                    IsFiltred = c.IsFiltred,
                }).ToList();

        return BuildBranch(null).Concat(BuildBranch(0)).ToList();
    }
}
