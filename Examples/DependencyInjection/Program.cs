using Microsoft.Extensions.DependencyInjection;

ServiceCollection services = new();

services.AddTransient<ITransientService, InMemoryDataRepository>();
services.AddScoped<IScopedService, InMemoryDataRepository>();
services.AddSingleton<ISingletonService, InMemoryDataRepository>();
services.AddSingleton<Example>();

IServiceProvider provider = services.BuildServiceProvider();

var example = provider.GetRequiredService<Example>();
await example.Run(provider);

public class Example()
{
    public async Task Run(IServiceProvider provider)
    {
        var scope = provider.CreateAsyncScope();
        var transientService = scope.ServiceProvider.GetRequiredService<ITransientService>();
        var scopedService = scope.ServiceProvider.GetRequiredService<IScopedService>();
        var singletonService = scope.ServiceProvider.GetRequiredService<ISingletonService>();

        Console.WriteLine("First Scope initial state:");
        LogState(transientService, scopedService, singletonService);

        await transientService.Delete(0);
        await scopedService.Delete(0);
        await singletonService.Delete(0);

        Console.WriteLine("\nFirst Scope updated state:");
        LogState(transientService, scopedService, singletonService);

        var transientService2 = scope.ServiceProvider.GetRequiredService<ITransientService>();
        var scopedService2 = scope.ServiceProvider.GetRequiredService<IScopedService>();
        var singletonService2 = scope.ServiceProvider.GetRequiredService<ISingletonService>();

        Console.WriteLine("\nFirst Scope newely requested services state:");
        //Transient is a new service and it's state should be reset
        //scoped and singleton are same services - states should retain
        LogState(transientService2, scopedService2, singletonService2);

        var scope2 = provider.CreateAsyncScope();
        var scope2TransientService = scope2.ServiceProvider.GetRequiredService<ITransientService>();
        var scope2ScopedService = scope2.ServiceProvider.GetRequiredService<IScopedService>();
        var scope2SingletonService = scope2.ServiceProvider.GetRequiredService<ISingletonService>();
        
        Console.WriteLine("\nNew scope");
        Console.WriteLine("Services state:");
        //Transient and scoped are new services and should have state reset
        //singleton is same service and it's state should retain
        LogState(scope2TransientService, scope2ScopedService, scope2SingletonService);
    }

    private void LogState(ITransientService transientService, IScopedService scopedService, ISingletonService singletonService)
    {
        Console.WriteLine($"Transient: {transientService.State()}");
        Console.WriteLine($"Scoped: {scopedService.State()}");
        Console.WriteLine($"Singleton: {singletonService.State()}");
    }
}


// In-memory data repository implementing all three service lifetimes just for demonstration of the difference.
// In a real-world scenario, you would typically have separate implementations for interfaces.
public class InMemoryDataRepository : ITransientService, IScopedService, ISingletonService
{
    private readonly List<string> _data =
    [
        "Item1",
        "Item2",
        "Item3"
    ];

    public async Task<List<string>> GetAllAsync() => _data;
    public async Task Delete(int id)
    {
        if (id < 0 || id >= _data.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "Invalid ID");
        }

        _data.RemoveAt(id);
    }

    public string State()
    {
        return $"Service-{this.GetHashCode().ToString()[4..]} Data: {string.Join(", ", _data)}";
    }
}


public interface ITransientService
{
    Task Delete(int id);
    Task<List<string>> GetAllAsync();
    string State();
}

public interface IScopedService
{
    Task Delete(int id);
    Task<List<string>> GetAllAsync();
    string State();
}

public interface ISingletonService
{
    Task Delete(int id);
    Task<List<string>> GetAllAsync();
    string State();
}