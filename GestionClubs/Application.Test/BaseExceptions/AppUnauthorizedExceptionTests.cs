using GestionClubs.Application.BaseExceptions;

namespace Application.Test.BaseExceptions;

public class AppUnauthorizedExceptionTests
{
    [Fact]
    public void Constructor_NoParameters_CreatesException()
    {
        var exception = new AppUnauthorizedException();

        Assert.NotNull(exception);
        Assert.IsAssignableFrom<Exception>(exception);
    }

    [Fact]
    public void Constructor_WithMessage_SetsMessage()
    {
        var message = "Unauthorized access";

        var exception = new AppUnauthorizedException(message);

        Assert.Equal(message, exception.Message);
    }

    [Fact]
    public void Constructor_WithMessageAndInnerException_SetsProperties()
    {
        var message = "Unauthorized access";
        var innerException = new Exception("Inner");

        var exception = new AppUnauthorizedException(message, innerException);

        Assert.Equal(message, exception.Message);
        Assert.Equal(innerException, exception.InnerException);
    }
}
