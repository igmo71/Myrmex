using Microsoft.Extensions.DependencyInjection;
using Myrmex.Core.Events;
using System.Reflection;

namespace Myrmex.EventDispatching;

public static class DependencyInjection
{
    public static IServiceCollection AddMyrmexEventDispatching(this IServiceCollection services, params Assembly[] handlerAssemblies)
    {
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        services.AddSingleton<IDomainEventHandlerRegistry, DomainEventHandlerRegistry>();

        if (handlerAssemblies.Length > 0)
            services.AddDomainEventHandlersFromAssemblies(handlerAssemblies);

        return services;
    }

    private static IServiceCollection AddDomainEventHandlersFromAssemblies(this IServiceCollection services, params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(assemblies);

        Type openHandlerType = typeof(IDomainEventHandler<>);

        IEnumerable<Type> handlerTypes = assemblies
            .SelectMany(assembly => assembly.DefinedTypes)
            .Where(type =>
                type is { IsAbstract: false, IsInterface: false } &&
                type.ImplementedInterfaces.Any(IsDomainEventHandlerInterface))
            .Select(type => type.AsType())
            .Distinct();

        foreach (Type handlerType in handlerTypes)
        {
            Type[] eventTypes = handlerType
                .GetInterfaces()
                .Where(IsDomainEventHandlerInterface)
                .Select(interfaceType => interfaceType.GetGenericArguments()[0])
                .Distinct()
                .ToArray();

            services.AddScoped(handlerType);

            foreach (Type eventType in eventTypes)
            {
                services.AddSingleton(new DomainEventHandlerDescriptor(eventType, handlerType));
            }
        }

        return services;
    }

    private static bool IsDomainEventHandlerInterface(Type type) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IDomainEventHandler<>);
}
