namespace ConsoleApp1;

public static class StoredProcedureTypeExtensions
{
    public static string GetProcedureName(this StoredProcedureType type)
    {
        return type switch
        {
            StoredProcedureType.GetVolnaCategories => "GetVolnaCategories",
            StoredProcedureType.GetOzonCategories => "GetOzonCategories",
            StoredProcedureType.GetCtCategories => "GetCtCategories",
            StoredProcedureType.SearchVolnaCategoryByName => "SearchVolnaCategoryByName",
            StoredProcedureType.SearchOzonCategoryByName => "SearchOzonCategoryByName",
            StoredProcedureType.SearchCtCategoryByName => "SearchCtCategoryByName",
            StoredProcedureType.SearchVolnaCategoryById => "SearchVolnaCategoryById",
            StoredProcedureType.SearchOzonCategoryById => "SearchOzonCategoryById",
            StoredProcedureType.SearchCtCategoryById => "SearchCtCategoryById",
            StoredProcedureType.AddCategoryLink => "AddCategoryLink",
            StoredProcedureType.DeleteCategoryLink => "DeleteCategoryLinkById",
            _ => throw new ArgumentException("Unknown procedure type")
        };
    }

    public static Dictionary<string, object> GetParameter(this StoredProcedureType type, object parameter)
    {
        var paramDict = new Dictionary<string, object>();

        if (parameter is not null)
        {
            switch (type)
            {
                case StoredProcedureType.SearchVolnaCategoryByName:
                case StoredProcedureType.SearchOzonCategoryByName:
                case StoredProcedureType.SearchCtCategoryByName:
                    if (parameter is string searchName)
                    {
                        paramDict["searchName"] = searchName;
                    }
                    break;

                case StoredProcedureType.SearchCtCategoryById:
                case StoredProcedureType.SearchVolnaCategoryById:
                case StoredProcedureType.SearchOzonCategoryById:
                    if (parameter is int searchId)
                    {
                        paramDict["categoryId"] = searchId;
                    }
                    break;
                
                case StoredProcedureType.AddCategoryLink:
                    if (parameter is (int ctCategoryId, int linkCategoryId, string linkTypeName))
                    {
                        paramDict["ct_category_id"] = ctCategoryId;
                        paramDict["link_category_id"] = linkCategoryId;
                        paramDict["link_type_name"] = linkTypeName;
                    }
                    break;
                case StoredProcedureType.DeleteCategoryLink: 
                    if (parameter is int categoryLinkId)
                    {
                        paramDict["CategoryLinkId"] = categoryLinkId;
                    }
                    break;
            }
        }

        return paramDict;
    }
}