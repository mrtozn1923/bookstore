namespace Bookstore.Api.Exceptions;

public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message)
    {
    }
}

public class NotFoundException : DomainException
{
    public NotFoundException(string resource, object key)
        : base($"{resource} with id {key} was not found.")
    {
    }
}

public class ConflictException : DomainException
{
    public ConflictException(string message) : base(message)
    {
    }
}
