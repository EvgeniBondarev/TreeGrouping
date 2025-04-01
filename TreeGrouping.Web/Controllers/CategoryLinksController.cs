using ConsoleApp1;
using Microsoft.AspNetCore.Mvc;
using TreeGrouping.Application.DbService;

namespace TreeGrouping.Web.Controllers;

public class CategoryLinksController : Controller
{
    private readonly IDatabaseService _dbService;

    public CategoryLinksController(IDatabaseService dbService)
    {
        _dbService = dbService;
    }
    
    
    [HttpGet]
    public async Task<IActionResult> CategoryLinks()
    {
        var categoryLinks = await _dbService.GetAllCategoryLinksAsync();
        return View(categoryLinks.OrderByDescending(c => c.Id).ToList());
    }
    
    [HttpPost]
    public async Task<IActionResult> DeleteCategoryLink(int id)
    {
        try
        {
            _dbService.ExecuteStoredProcedureAsync(StoredProcedureType.DeleteCategoryLink, id);
            return Json(new { success = true, message = "Связка успешно удалена" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Ошибка при удалении связки: " + ex.Message });
        }
    }

}