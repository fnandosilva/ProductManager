using MediatR;
using Microsoft.AspNetCore.Mvc;
using ProductManager.Application.Auth.Commands.Login;
using ProductManager.Application.Auth.Commands.Register;

namespace ProductManager.Presentation.Auth;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ISender _sender;

    public AuthController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var command = new RegisterCommand(request.Username, request.Email, request.Password);
        var response = await _sender.Send(command, cancellationToken);

        return Ok(response);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var command = new LoginCommand(request.Email, request.Password);
        var response = await _sender.Send(command, cancellationToken);

        return Ok(response);
    }
}
