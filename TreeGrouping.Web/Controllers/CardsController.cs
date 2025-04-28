using Microsoft.AspNetCore.Mvc;

namespace TreeGrouping.Web.Controllers;

public class CardsController : Controller
{
    public IActionResult Index(string typeName)
    {
        ViewBag.TypeName = typeName ?? string.Empty;
        return View();
    }
}