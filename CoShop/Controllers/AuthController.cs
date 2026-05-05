using Microsoft.AspNetCore.Mvc;
using CoShop.DTOs;
using CoShop.Models;
using CoShop.Repositories;
using CoShop.Services;

namespace CoShop.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IUserRepository _users;
    private readonly IJwtService _jwt;

    public AuthController(IUserRepository users, IJwtService jwt)
    {
        _users = users;
        _jwt   = jwt;
    }

    /// <summary>Neuen Benutzeraccount registrieren.</summary>
    /// <remarks>
    /// Passwort-Anforderungen: mind. 8 Zeichen, 1 Grossbuchstabe, 1 Kleinbuchstabe, 1 Zahl.
    /// </remarks>
    /// <response code="200">Registrierung erfolgreich, JWT Token zurückgegeben.</response>
    /// <response code="409">E-Mail oder Benutzername bereits vergeben.</response>
    /// <response code="400">Validierungsfehler in den Eingabefeldern.</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), 200)]
    [ProducesResponseType(409)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest req)
    {
        if (await _users.EmailExistsAsync(req.Email))
            return Conflict(new { message = "E-Mail ist bereits vergeben." });

        if (await _users.UsernameExistsAsync(req.Username))
            return Conflict(new { message = "Benutzername ist bereits vergeben." });

        var user = new User
        {
            Username     = req.Username.Trim(),
            Email        = req.Email.Trim().ToLower(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
            Role         = UserRole.User
        };

        await _users.AddAsync(user);
        await _users.SaveChangesAsync();

        return Ok(new AuthResponse(_jwt.GenerateToken(user), user.Id, user.Username, user.Role.ToString()));
    }

    /// <summary>Mit E-Mail und Passwort anmelden und JWT Token erhalten.</summary>
    /// <response code="200">Login erfolgreich, JWT Token zurückgegeben.</response>
    /// <response code="401">Ungültige Anmeldedaten.</response>
    /// <response code="400">Validierungsfehler.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest req)
    {
        var user = await _users.GetByEmailAsync(req.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            return Unauthorized(new { message = "Ungültige Anmeldedaten." });

        return Ok(new AuthResponse(_jwt.GenerateToken(user), user.Id, user.Username, user.Role.ToString()));
    }
}