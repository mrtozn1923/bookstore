namespace Bookstore.Api.Services;

public abstract class Operation
{
    // Generates a short ID the moment the instance is created
    public string Id { get; } = Guid.NewGuid().ToString()[..8];
}

public class SingletonOperation : Operation { }
public class ScopedOperation : Operation { }
public class TransientOperation : Operation { }

public class OperationReporter
{
    private readonly SingletonOperation _singleton;
    private readonly ScopedOperation _scoped;
    private readonly TransientOperation _transient;

    public OperationReporter(SingletonOperation singleton, ScopedOperation scoped, TransientOperation transient)
    {
        _singleton = singleton;
        _scoped = scoped;
        _transient = transient;
    }

    public object Report() => new { singleton = _singleton.Id, scoped = _scoped.Id, transient = _transient.Id };
}
