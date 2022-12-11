namespace SampleApplication.Client.Types;

public class SomeClassWithUser
{
    public string SomeOtherInformation { get; set; }
    public int Number { get; set; }

    public UserModel MainUser { get; set; } = new() {FirstName = "Don", LastName = "Joe", Password = "APassword"};
    
    public List<UserModel> Users { get; set; } = new()
    {
        new UserModel { FirstName = "John", LastName = "Doe", Password = "APassword" },
    };
}