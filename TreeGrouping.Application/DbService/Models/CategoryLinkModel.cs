namespace TreeGrouping.Application.DbService.Models;

public class CategoryLinkModel
{
    public int Id { get; set; }
    public int? VolnaCategoryId { get; set; }
    public long? OzonCategoryId { get; set; }
    public int? CtCategoryId { get; set; }
}