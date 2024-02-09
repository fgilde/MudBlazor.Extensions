namespace MudBlazor.Extensions.Attribute;

/// <summary>
/// Don't know why I need this but however MudBlazor throw's an exception if the category is not valid
/// So I created this attribute to prevent this exception.
/// </summary>
public class SafeCategoryAttribute : CategoryAttribute
{
    string _name;
    /// <summary>
    /// Constructor
    /// </summary>
    public SafeCategoryAttribute(string name) : base(GetValidCategory(name))
    {
        _name = name;
    }

    public new string Name => _name;
    
    private static string GetValidCategory(string name)
    {
        try
        {
            // Try to create a CategoryAttribute with the provided name.
            // If it doesn't throw an exception, the name is valid.
            var categoryAttribute = new CategoryAttribute(name);

            // If we've made it this far without an exception, return the name as-is.
            return name;
        }
        catch (ArgumentException)
        {
            // If an ArgumentException was thrown, the name is not valid.
            // Return "Misc" as a safe default.
            return CategoryTypes.Element.Misc;
        }
    }
}