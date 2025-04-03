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
    private async Task<List<CategoryModel>> GetCategoriesWithCache(string cacheKey,
        StoredProcedureType storedProcedureType, string name)
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
        var newCategories = categories.ToList();
        var links = await _dbService.GetAllCategoryLinksAsync();
        var categoryLinkToModels = await CategoryLinkToModel(links.ToList()); 
        newCategories.AddRange(categoryLinkToModels);
        var tree = BuildTree(newCategories);
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
    public async Task<IActionResult> LinkCategories([FromBody] Dictionary<string, int> selectedCategories)
    {
        try
        {
            // Проверка, что словарь не пустой и содержит ключ "CT"
            if (selectedCategories == null || selectedCategories.Count <= 1 || !selectedCategories.ContainsKey("CT"))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Ошибка: Должен быть выбран элемент с ключом 'CT' и должно быть более одной категории."
                });
            }

            // Извлекаем значение для CT
            var ctValue = selectedCategories["CT"];

            // Создаем список для хранения новых словарей
            var categoryPairs = new List<Dictionary<string, int>>();

            // Перебираем все ключи в словаре, кроме "CT"
            foreach (var key in selectedCategories.Keys.Where(k => k != "CT"))
            {
                // Создаем новый словарь с ключами "CT" и текущим элементом
                var pair = new Dictionary<string, int>
                {
                    { "CT", ctValue },
                    { key, selectedCategories[key] }
                };

                // Добавляем пару в список
                categoryPairs.Add(pair);
            }

            // Проходим по списку категорий и выполняем запросы для каждой пары
            foreach (var pair in categoryPairs)
            {
                // Получаем значение для ключа "CT"
                int ct = pair.ContainsKey("CT") ? pair["CT"] : 0;

                // Получаем второй элемент, который не является "CT"
                var secondKey = pair.Keys.FirstOrDefault(k => k != "CT");
                var secondValue = secondKey != null ? pair[secondKey] : 0;

                // Выполняем хранимую процедуру для каждой пары
                await _dbService.ExecuteStoredProcedureAsync(
                    StoredProcedureType.AddCategoryLink,
                    (ct, secondValue, secondKey)
                );
            }

            // Возвращаем положительный ответ
            return Ok(new { success = true, message = "Категории успешно связаны!", data = categoryPairs });
        }
        catch (Exception ex)
        {
            // Возвращаем ошибку, если что-то пошло не так
            return BadRequest(new
                { success = false, message = "Ошибка при связывании категорий!", error = ex.Message });
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
                    ParentName = c.ParentName,
                    Children = c.Children.Any() ? c.Children : BuildBranch(c.Id)
                }).ToList();

        return BuildBranch(null).Concat(BuildBranch(0)).ToList();
    }


    private async Task<List<CategoryModel>> CategoryLinkToModel(List<CategoryLinkModel> links)
    {
        var tasks = links.Select(async link =>
        {
            if (!Enum.TryParse<CategoryLinkType>(link.LinkTypeName, true, out var linkType))
            {
                return Enumerable.Empty<CategoryModel>();
            }

            var linkTreeTask = await _dbService.ExecuteStoredProcedureAsync(linkType.GetProcedureType(), link.LinkCategoryId);
            var linkTree = BuildTree(linkTreeTask.ToList());

            if (linkTree.Any())
            {
                var first = linkTree.First();
                first.ParentId = link.CtCategoryId;
                first.ParentName = link.LinkTypeName;
            }

            return linkTree;
        });

        var combinedLinkTrees = (await Task.WhenAll(tasks)).SelectMany(t => t).ToList();
        return combinedLinkTrees;
    }
}
