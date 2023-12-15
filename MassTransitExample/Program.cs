using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace MassTransitExample;

public static class State
{
    public static bool Event1Processed;
    public static bool Event2Processed;
}

public record Event1;

public class Subscriber1 : IConsumer<Event1>
{
    public async Task Consume(ConsumeContext<Event1> context)
    {
        await Task.Delay(3000);
        State.Event1Processed = true;
    }
}

public record Event2;

public class Subscriber2 : IConsumer<Event2>
{
    public async Task Consume(ConsumeContext<Event2> context)
    {
        await Task.Delay(3000);
        State.Event2Processed = true;
    }
}

public static class Program
{
    public static async Task Main()
    {
        // Arrange
        // Create first harness
        var subscriber1 = new Subscriber1();
        var provider1 = new ServiceCollection()
            .AddSingleton(subscriber1)
            .AddMassTransitTestHarness(x =>
            {
                x.AddConsumer<Subscriber1>();
            })
            .BuildServiceProvider();
        
        var harness1 = provider1.GetRequiredService<ITestHarness>();
        await harness1.Start();
        
        // Create second harness
        var subscriber2 = new Subscriber2();
        var provider2 = new ServiceCollection()
            .AddSingleton(subscriber2)
            .AddMassTransitTestHarness(x =>
            {
                x.AddConsumer<Subscriber2>();
            })
            .BuildServiceProvider();
        
        var harness2 = provider2.GetRequiredService<ITestHarness>();
        await harness2.Start();
        
        // Act
        
        // Test 1
        await harness1.Bus.Publish(new Event1());
        await harness1.Consumed.Any<Event1>(); // This waits for 3 seconds

        // Test 2
        await harness2.Bus.Publish(new Event2());
        await harness2.Consumed.Any<Event2>(); // This completes immediately, without waiting for the message to be consumed

        // Assert
        
        // Test 1
        if (!State.Event1Processed)
        {
            throw new Exception("Event 1 not processed");
        }

        // Test 2
        if (!State.Event2Processed)
        {
            throw new Exception("Event 2 not processed"); // This throws
        }
        
        await provider1.DisposeAsync();
        await provider2.DisposeAsync();
    }
}
