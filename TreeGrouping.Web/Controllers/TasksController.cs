using Microsoft.AspNetCore.Mvc;

namespace TreeGrouping.Web.Controllers;

public class TasksController: Controller
{
    public IActionResult Index()
    {
        return View();
    }
}