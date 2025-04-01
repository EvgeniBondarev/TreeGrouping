using TreeGrouping.Application.DbService.Models;

namespace TreeGrouping.Web.Models;

public class CategoryViewModel
{
    public List<CategoryModel> Categories { get; set; } = new();
}