using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Janitor.Tests;

public class ServiceRegistrationTests
{
    [Fact]
    public void ShouldAddJanitorWithConfig()
    {
        var configInvoked = false;
        var serviceCollection = new ServiceCollection();

        serviceCollection.AddLogging(lb =>
        {
            lb.AddConsole().SetMinimumLevel(LogLevel.Debug);
        });

        serviceCollection.AddJanitor((sp, janitor) => configInvoked = true);
        var serviceProvider = serviceCollection.BuildServiceProvider();
        var janitor = serviceProvider.GetService<IJanitor>();
        configInvoked.Should().BeTrue();
    }

    [Fact]
    public void ShouldAddJanitorWithoutConfig()
    {
        var serviceCollection = new ServiceCollection();

        serviceCollection.AddLogging(lb =>
        {
            lb.AddConsole().SetMinimumLevel(LogLevel.Debug);
        });

        serviceCollection.AddJanitor();
        var serviceProvider = serviceCollection.BuildServiceProvider();
        serviceProvider.GetService<IJanitor>();
    }
}