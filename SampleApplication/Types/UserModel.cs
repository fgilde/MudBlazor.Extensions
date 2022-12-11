namespace SampleApplication.Client.Types;

public class UserModel
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Password { get; set; }

    public override string ToString() => $"{FirstName} {LastName}";
}