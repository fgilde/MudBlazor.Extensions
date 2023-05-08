namespace MudBlazor.Extensions.Attribute;


public class BetaAttribute: System.Attribute 
{
    public BetaAttribute(string message)
    {
        Message = message;
    }

    public string Message { get; set; }
}