using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Myrmex.EventDispatching;

public static class DependencyInjection
{
    public static IServiceCollection AddEventDispatching(this IServiceCollection services, params Assembly[] handlerAssemblies)
    {
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        services.AddSingleton<IDomainEventHandlerRegistry, DomainEventHandlerRegistry>();

        if (handlerAssemblies.Length > 0)
            services.AddDomainEventHandlersFromAssemblies(handlerAssemblies);

        return services;
    }
}
