using System.Security.Claims;

namespace CoShop.Controllers;

public static class ClaimsPrincipalExtensions
{
    public static int GetUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? throw new InvalidOperationException("User ID claim missing.");
        return int.Parse(value);
    }
}