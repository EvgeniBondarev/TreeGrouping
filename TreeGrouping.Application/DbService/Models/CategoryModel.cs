

namespace TreeGrouping.Application.DbService.Models;

public class CategoryModel
{
    public int Id { get; set; }
    public int? ParentId { get; set; }
    public string Name { get; set; }
    public List<CategoryModel> Children { get; set; } = new();
    public string? ParentName { get; set; }
    public bool IsFiltred { get; set; } = false;
}