using System.Security.Claims;

namespace ChoreTracker.API.Services;

public interface ICurrentUserService
{
    int? GetUserId();
    int GetUserIdOrThrow();
}

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public int? GetUserId()
    {
        var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
        {
            return userId;
        }
        return null;
    }

    public int GetUserIdOrThrow()
    {
        var userId = GetUserId();
        if (userId == null)
        {
            throw new UnauthorizedAccessException("User is not authenticated");
        }
        return userId.Value;
    }
}
