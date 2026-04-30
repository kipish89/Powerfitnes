using Microsoft.AspNetCore.Mvc;
using Moq;
using PowerFitness.Api.Controllers;
using PowerFitness.Api.Models;
using PowerFitness.Api.Services;
using Xunit;

namespace PowerFitness.Api.Tests.Controllers;

public sealed class AuthControllerTests
{
    [Fact]
    public async Task Register_ReturnsBadRequest_WhenServiceThrowsInvalidOperation()
    {
        var repository = new Mock<IFitnessRepository>();
        var telegramService = new Mock<ITelegramRegistrationService>();
        var authService = new Mock<IAuthService>();

        authService
            .Setup(x => x.RegisterAsync(It.IsAny<RegisterRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("bad request"));

        var controller = new AuthController(repository.Object, telegramService.Object, authService.Object);
        var result = await controller.Register(new RegisterRequest(), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_WhenCredentialsAreInvalid()
    {
        var repository = new Mock<IFitnessRepository>();
        var telegramService = new Mock<ITelegramRegistrationService>();
        var authService = new Mock<IAuthService>();

        authService
            .Setup(x => x.LoginAsync(It.IsAny<LoginRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AuthResponse?)null);

        var controller = new AuthController(repository.Object, telegramService.Object, authService.Object);
        var result = await controller.Login(new LoginRequest(), CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }
}
