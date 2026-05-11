using GestionClubs.Application.BaseExceptions;

namespace Application.Test.BaseExceptions;

public class EntityNotFoundExceptionTests
{
    [Fact]
    public void Constructor_NoParameters_CreatesException()
    {
        var exception = new EntityNotFoundException();

        Assert.NotNull(exception);
        Assert.IsAssignableFrom<Exception>(exception);
    }

    [Fact]
    public void Constructor_WithMessage_SetsMessage()
    {
        var message = "Entity not found";

        var exception = new EntityNotFoundException(message);

        Assert.Equal(message, exception.Message);
    }

    [Fact]
    public void Constructor_WithMessageAndInnerException_SetsProperties()
    {
        var message = "Entity not found";
        var innerException = new Exception("Inner");

        var exception = new EntityNotFoundException(message, innerException);

        Assert.Equal(message, exception.Message);
        Assert.Equal(innerException, exception.InnerException);
    }
}
