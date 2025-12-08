using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ClassLibrary1;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the library services and binds configuration automatically.
    /// This encapsulates the library's complexity from the consumer.
    /// </summary>
    public static IServiceCollection AddHelloDotnetLibrary(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Bind Options
        // We look for sections named "Class1" and "Class2" in the root config
        services.Configure<Class1Options>(options => configuration.GetSection(Class1Options.SectionName).Bind(options));
        services.Configure<Class2Options>(options => configuration.GetSection(Class2Options.SectionName).Bind(options));

        // 2. Register Services
        // We register interfaces, allowing the implementation to change without breaking consumers
        services.AddSingleton<IClass1, Class1>();
        services.AddSingleton<IClass2, Class2>();

        return services;
    }
}
