using IdentityProvider.Exceptions;
using IdentityProvider.Handlers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using Xunit;

namespace IdentityProvider.Tests.Handlers;

public class GlobalExceptionHandlerTests
{
    private readonly Mock<ILogger<GlobalExceptionHandler>> _loggerMock;
    private readonly GlobalExceptionHandler _sut;

    public GlobalExceptionHandlerTests()
    {
        _loggerMock = new Mock<ILogger<GlobalExceptionHandler>>();
        _sut = new GlobalExceptionHandler(_loggerMock.Object);
    }

    [Theory]
    [MemberData(nameof(ExceptionTestCases))]
    public async Task TryHandleAsync_ReturnsCorrectStatusCode(Exception exception, HttpStatusCode expectedStatusCode)
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();

        // Act
        var result = await _sut.TryHandleAsync(httpContext, exception, CancellationToken.None);

        // Assert
        Assert.True(result);
        Assert.Equal((int)expectedStatusCode, httpContext.Response.StatusCode);
    }

    public static TheoryData<Exception, HttpStatusCode> ExceptionTestCases => new()
    {
        { new LoginFailedException("test@test.com"), HttpStatusCode.Unauthorized },
        { new UserAlreadyExistsException("test@test.com"), HttpStatusCode.Conflict },
        { new RegistrationFailedException(["Error1"]), HttpStatusCode.BadRequest },
        { new RefreshTokenException("Invalid token"), HttpStatusCode.Unauthorized },
        { new InvalidOperationException("Unknown"), HttpStatusCode.InternalServerError }
    };
}