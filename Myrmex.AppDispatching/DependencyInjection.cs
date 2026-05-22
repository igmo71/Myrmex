using Microsoft.Extensions.DependencyInjection;
using Myrmex.AppDispatching.CommandDispatching;
using Myrmex.AppDispatching.EventDispatching;
using Myrmex.AppDispatching.QueryDispatching;
using Myrmex.Core.Application;
using Myrmex.Core.Events;
using System.Reflection;

namespace Myrmex.AppDispatching;

public static class DependencyInjection
{
    public static IServiceCollection AddCommandAndQueryDispatching(this IServiceCollection services, params Assembly[] handlerAssemblies)
    {
        services.AddScoped<ICommandDispatcher, CommandDispatcher>();

        services.AddScoped<IQueryDispatcher, QueryDispatcher>();

        if (handlerAssemblies.Length > 0)
        {
            services.AddApplicationHandlersFromAssemblies(handlerAssemblies);
        }

        return services;
    }

    public static IServiceCollection AddDomainEventDispatching(this IServiceCollection services, params Assembly[] handlerAssemblies)
    {
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        services.AddSingleton<IDomainEventHandlerRegistry, DomainEventHandlerRegistry>();

        if (handlerAssemblies.Length > 0)
        {
            services.AddDomainEventHandlersFromAssemblies(handlerAssemblies);
        }

        return services;
    }

    public static IServiceCollection AddApplicationHandlersFromAssemblies(this IServiceCollection services, params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(assemblies);

        Type openCommandHandlerType = typeof(ICommandHandler<,>);
        Type openQueryHandlerType = typeof(IQueryHandler<,>);

        IEnumerable<Type> handlerTypes = assemblies
            .SelectMany(assembly => assembly.DefinedTypes)
            .Where(type =>
                type is { IsAbstract: false, IsInterface: false } &&
                type.ImplementedInterfaces.Any(IsApplicationHandlerInterface))
            .Select(type => type.AsType())
            .Distinct();

        foreach (Type handlerType in handlerTypes)
        {
            Type[] handlerInterfaces = handlerType
                .GetInterfaces()
                .Where(IsApplicationHandlerInterface)
                .Distinct()
                .ToArray();

            foreach (Type handlerInterface in handlerInterfaces)
            {
                services.AddScoped(handlerInterface, handlerType);
            }
        }

        return services;

        bool IsApplicationHandlerInterface(Type type) =>
            type.IsGenericType &&
            (type.GetGenericTypeDefinition() == openCommandHandlerType ||
             type.GetGenericTypeDefinition() == openQueryHandlerType);
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

        static bool IsDomainEventHandlerInterface(Type type) =>
            type.IsGenericType &&
            type.GetGenericTypeDefinition() == typeof(IDomainEventHandler<>);
    }
}

