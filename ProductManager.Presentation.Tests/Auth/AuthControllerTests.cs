using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ProductManager.Application.Auth.Commands.Login;
using ProductManager.Application.Auth.Commands.Refresh;
using ProductManager.Application.Auth.Commands.Register;
using ProductManager.Application.Auth.Commands.Revoke;
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
        var response = new AuthResponse("fake-token", "johndoe", "john@example.com", "refresh-token");
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
        var response = new AuthResponse("fake-token", "johndoe", "john@example.com", "refresh-token");
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

    [Fact]
    public async Task Refresh_ShouldSendRefreshTokenCommandAndReturnOkWithResponse()
    {
        var response = new AuthResponse("new-token", "johndoe", "john@example.com", "new-refresh");
        _sender.Setup(s => s.Send(It.IsAny<RefreshTokenCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _controller.Refresh(new RefreshTokenRequest("old-refresh"), CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeSameAs(response);

        _sender.Verify(
            s => s.Send(It.Is<RefreshTokenCommand>(c => c.RefreshToken == "old-refresh"), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Revoke_ShouldSendRevokeTokenCommandAndReturnOk()
    {
        _sender.Setup(s => s.Send(It.IsAny<RevokeTokenCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(Unit.Value));

        var result = await _controller.Revoke(new RefreshTokenRequest("refresh-token"), CancellationToken.None);

        result.Should().BeOfType<OkResult>();
        _sender.Verify(
            s => s.Send(It.Is<RevokeTokenCommand>(c => c.RefreshToken == "refresh-token"), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
