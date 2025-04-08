using ConsoleApp1;
using TreeGrouping.Application.DbService;
using TreeGrouping.Application.DbService.Models;

namespace TreeGrouping.Application.CategoryService;

public class CategoryTreeService
{
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
                    LinkId = c.LinkId,
                }).ToList();

        return BuildBranch(null).Concat(BuildBranch(0)).ToList();
    }


    public async Task<List<CategoryModel>> CategoryLinkToModel(IDatabaseService dbService,List<CategoryLinkModel> links)
    {
        var tasks = links.Select(async link =>
        {
            if (!Enum.TryParse<CategoryLinkType>(link.LinkTypeName, true, out var linkType))
            {
                return Enumerable.Empty<CategoryModel>();
            }

            if (link.LinkTypeName == "IC")
            {
                
            }

            var linkTreeTask = await dbService.ExecuteStoredProcedureAsync(linkType.GetProcedureType(), link.LinkCategoryId);
            
            var linkTree = BuildTree(linkTreeTask.ToList());

            if (linkTree.Any())
            {
                var first = linkTree.First();
                first.ParentId = link.CtCategoryId;
                first.ParentName = link.LinkTypeName;
                first.LinkId = link.Id;
            }

            return linkTree;
        });

        var combinedLinkTrees = (await Task.WhenAll(tasks)).SelectMany(t => t).ToList();
        return combinedLinkTrees;
    }
}