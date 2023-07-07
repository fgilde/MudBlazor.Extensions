namespace MudBlazor.Extensions.Attribute;

/// <summary>
/// Attribute to mark a class as beta
/// </summary>
public class BetaAttribute: System.Attribute 
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="message"></param>
    public BetaAttribute(string message)
    {
        Message = message;
    }

    /// <summary>
    /// Message
    /// </summary>
    public string Message { get; set; }
}