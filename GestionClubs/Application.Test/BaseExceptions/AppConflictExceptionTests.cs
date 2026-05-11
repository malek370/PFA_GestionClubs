using GestionClubs.Application.BaseExceptions;

namespace Application.Test.BaseExceptions;

public class AppConflictExceptionTests
{
    [Fact]
    public void Constructor_NoParameters_CreatesException()
    {
        var exception = new AppConflictException();

        Assert.NotNull(exception);
        Assert.IsAssignableFrom<Exception>(exception);
    }

    [Fact]
    public void Constructor_WithMessage_SetsMessage()
    {
        var message = "Conflict occurred";

        var exception = new AppConflictException(message);

        Assert.Equal(message, exception.Message);
    }

    [Fact]
    public void Constructor_WithMessageAndInnerException_SetsProperties()
    {
        var message = "Conflict occurred";
        var innerException = new Exception("Inner");

        var exception = new AppConflictException(message, innerException);

        Assert.Equal(message, exception.Message);
        Assert.Equal(innerException, exception.InnerException);
    }
}
