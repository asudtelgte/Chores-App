namespace ChoreTracker.API.GraphQL.Inputs;

public class LoginInput
{
    public required string Email { get; set; }
    public required string Password { get; set; }
}
