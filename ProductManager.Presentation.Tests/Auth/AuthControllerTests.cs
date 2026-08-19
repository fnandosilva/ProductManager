using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ProductManager.Application.Auth.Commands.Login;
using ProductManager.Application.Auth.Commands.Register;
using ProductManager.Application.Auth.Dtos;
using ProductManager.Presentation.Auth;

namespace ProductManager.Presentation.Tests.Auth;

public class AuthControllerTests
{
    private readonly Mock<ISender> _sender = new();
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _controller = new AuthController(_sender.Object);
    }

    [Fact]
    public async Task Register_ShouldSendRegisterCommandAndReturnOkWithResponse()
    {
        var response = new AuthResponse("fake-token", "johndoe", "john@example.com");
        _sender.Setup(s => s.Send(It.IsAny<RegisterCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var request = new RegisterRequest("johndoe", "john@example.com", "password123");
        var result = await _controller.Register(request, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeSameAs(response);

        _sender.Verify(
            s => s.Send(
                It.Is<RegisterCommand>(c =>
                    c.Username == "johndoe" && c.Email == "john@example.com" && c.Password == "password123"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Login_ShouldSendLoginCommandAndReturnOkWithResponse()
    {
        var response = new AuthResponse("fake-token", "johndoe", "john@example.com");
        _sender.Setup(s => s.Send(It.IsAny<LoginCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var request = new LoginRequest("john@example.com", "password123");
        var result = await _controller.Login(request, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeSameAs(response);

        _sender.Verify(
            s => s.Send(
                It.Is<LoginCommand>(c => c.Email == "john@example.com" && c.Password == "password123"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
