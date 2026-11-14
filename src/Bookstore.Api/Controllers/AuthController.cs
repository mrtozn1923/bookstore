using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bookstore.Api.Auth;

namespace Bookstore.Api.Controllers;

public record LoginRequest(string Email, string Password);

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserStore _users;
    private readonly ITokenService _tokens;

    public AuthController(UserStore users, ITokenService tokens)
    {
        _users = users;
        _tokens = tokens;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public ActionResult<TokenResponse> Login(LoginRequest request)
    {
        var user = _users.ValidateCredentials(request.Email, request.Password);
        if (user is null)
        {
            // we don't say which piece of information was wrong
            return Unauthorized();
        }

        return Ok(_tokens.CreateAccessToken(user));
    }

    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        var claims = User.Claims.Select(c => new { c.Type, c.Value });
        return Ok(claims);
    }
}
