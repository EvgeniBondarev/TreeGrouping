using ConsoleApp1;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using TreeGrouping.Application.DbService;
using TreeGrouping.Application.DbService.Models;
using TreeGrouping.Web.Models;

namespace TreeGrouping.Web.Controllers;

public class CategoryController : Controller
{
    private readonly IDatabaseService _dbService;
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(30); // Длительность кэша

    public CategoryController(IDatabaseService dbService, IMemoryCache cache)
    {
        _dbService = dbService;
        _cache = cache;
    }

    public async Task<IActionResult> Index()
    {
        return View();
    }

    // Получение категорий с кэшем и фильтрацией
    private async Task<List<CategoryModel>> GetCategoriesWithCache(string cacheKey, StoredProcedureType storedProcedureType, string name)
    {
        if (!_cache.TryGetValue(cacheKey, out List<CategoryModel> categories))
        {
            // Данные не найдены в кэше, загружаем их из БД
            categories = (await _dbService.ExecuteStoredProcedureAsync(storedProcedureType)).ToList();
            _cache.Set(cacheKey, categories, _cacheDuration);
        }

        // Если фильтр не задан — возвращаем все категории
        if (string.IsNullOrEmpty(name))
        {
            return categories;
        }

        // Фильтруем категории по названию
        var filteredCategories = categories
            .Where(c => c.Name.Contains(name, StringComparison.OrdinalIgnoreCase))
            .ToList();

        // Собираем всех родительских категорий для найденных элементов
        var parentIds = new HashSet<int?>(filteredCategories.Select(c => c.ParentId));
        var allRelevantCategories = new HashSet<CategoryModel>(filteredCategories);

        while (parentIds.Count > 0)
        {
            var newParents = categories
                .Where(c => parentIds.Contains(c.Id) && !allRelevantCategories.Contains(c))
                .ToList();

            if (!newParents.Any())
            {
                break;
            }

            foreach (var parent in newParents)
            {
                allRelevantCategories.Add(parent);
            }

            // Обновляем список родительских ID, чтобы подниматься по иерархии
            parentIds = new HashSet<int?>(newParents.Select(c => c.ParentId));
        }

        return allRelevantCategories.ToList();
    }


    // Метод для получения Volna категорий
    [HttpGet]
    public async Task<IActionResult> GetVolnaCategories(string name = null)
    {
        var categories = await GetCategoriesWithCache("volna_categories", StoredProcedureType.GetVolnaCategories, name);
        var tree = BuildTree(categories);
        var model = Tuple.Create(tree, "Volna");

        return PartialView("_CategoryTree", model);
    }
    
    [HttpGet]
    public async Task<IActionResult> GetVolnaCategoriesById(int id)
    {
        var categories = await _dbService.ExecuteStoredProcedureAsync(StoredProcedureType.SearchVolnaCategoryById, id);
        var tree = BuildTree(categories.ToList());
        var model = Tuple.Create(tree, "Volna");

        return PartialView("_CategoryTree", model);
    }

    // Метод для получения Ozon категорий
    [HttpGet]
    public async Task<IActionResult> GetOzonCategories(string name = null)
    {
        var categories = await GetCategoriesWithCache("ozon_categories", StoredProcedureType.GetOzonCategories, name);
        var tree = BuildTree(categories);
        var model = Tuple.Create(tree, "Ozon");

        return PartialView("_CategoryTree", model);
    }
    
    [HttpGet]
    public async Task<IActionResult> GetOzonCategoriesById(int id)
    {
        var categories = await _dbService.ExecuteStoredProcedureAsync(StoredProcedureType.SearchOzonCategoryById, id);
        var tree = BuildTree(categories.ToList());
        var model = Tuple.Create(tree, "Volna");

        return PartialView("_CategoryTree", model);
    }

    // Метод для получения CT категорий
    [HttpGet]
    public async Task<IActionResult> GetCtCategories(string name = null)
    {
        var categories = await GetCategoriesWithCache("ct_categories", StoredProcedureType.GetCtCategories, name);
        var tree = BuildTree(categories);
        var model = Tuple.Create(tree, "CT");

        return PartialView("_CategoryTree", model);
    }
    [HttpGet]
    public async Task<IActionResult> GetCtCategoriesById(int id)
    {
        var categories = await _dbService.ExecuteStoredProcedureAsync(StoredProcedureType.SearchCtCategoryById, id);
        var tree = BuildTree(categories.ToList());
        var model = Tuple.Create(tree, "Volna");

        return PartialView("_CategoryTree", model);
    }
    
    [HttpPost]
    public async Task<IActionResult> LinkCategories([FromBody] Dictionary<string, string> selectedCategories)
    {
        if (selectedCategories == null || selectedCategories.Count != 3)
        {
            return BadRequest(new { message = "Нужно выбрать ровно 3 категории!" });
        }

        try
        {
            int volnaCategoryId = int.Parse(selectedCategories.GetValueOrDefault("Volna", "0"));
            long ozonCategoryId = long.Parse(selectedCategories.GetValueOrDefault("Ozon", "0"));
            int ctCategoryId = int.Parse(selectedCategories.GetValueOrDefault("CT", "0"));
            var categories = await _dbService.ExecuteStoredProcedureAsync(
                StoredProcedureType.AddCategoryLink,
                (volnaCategoryId, ozonCategoryId, ctCategoryId)
            );

            return Json(new { success = true, message = "Категории успешно связаны!" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = "Ошибка при связывании категорий!", error = ex.Message });
        }
    }

    private List<CategoryModel> BuildTree(List<CategoryModel> categories)
    {
        var lookup = categories.ToLookup(c => c.ParentId);

        List<CategoryModel> BuildBranch(int? parentId) =>
            lookup[parentId]
                .Select(c => new CategoryModel
                {
                    Id = c.Id,
                    ParentId = c.ParentId,
                    Name = c.Name,
                    Children = BuildBranch(c.Id)
                }).ToList();

        return BuildBranch(null).Concat(BuildBranch(0)).ToList();
    }
}
