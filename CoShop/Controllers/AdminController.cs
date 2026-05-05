using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CoShop.DTOs;
using CoShop.Repositories;

namespace CoShop.Controllers;

/// <summary>Admin-only endpoints. Requires Role = Admin in JWT.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
[Produces("application/json")]
public class AdminController : ControllerBase
{
    private readonly IUserRepository _users;

    public AdminController(IUserRepository users) => _users = users;

    /// <summary>Alle Benutzer auflisten (nur Admin).</summary>
    /// <response code="200">Liste aller Benutzer.</response>
    /// <response code="403">Zugriff verweigert — kein Admin.</response>
    [HttpGet("users")]
    [ProducesResponseType(typeof(IEnumerable<UserDto>), 200)]
    [ProducesResponseType(403)]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetAllUsers()
    {
        var users = await _users.GetAllAsync();
        return Ok(users.Select(u => new UserDto(u.Id, u.Username, u.Email, u.Role.ToString())));
    }
}