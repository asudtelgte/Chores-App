using ChoreTracker.API.Models;

namespace ChoreTracker.API.GraphQL.Types;

public class AuthPayload
{
    public string Token { get; set; } = string.Empty;
    public User User { get; set; } = null!;
}
