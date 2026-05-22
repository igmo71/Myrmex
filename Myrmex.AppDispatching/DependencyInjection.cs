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
    public static IServiceCollection AddAppDispatching(this IServiceCollection services, params Assembly[] handlerAssemblies)
    {
        services.AddCommandAndQueryDispatching(handlerAssemblies);

        services.AddDomainEventDispatching(handlerAssemblies);

        return services;
    }

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

        var registrations = handlerTypes
            .SelectMany(handlerType => handlerType
                .GetInterfaces()
                .Where(IsApplicationHandlerInterface)
                .Select(handlerInterface => new
                {
                    HandlerInterface = handlerInterface,
                    HandlerType = handlerType
                }))
            .ToArray();

        var duplicates = registrations
            .GroupBy(x => x.HandlerInterface)
            .Where(g => g.Count() > 1)
            .ToArray();

        if (duplicates.Length > 0)
        {
            string message = string.Join(Environment.NewLine,
                duplicates.Select(group => $"{group.Key.Name}: {string.Join(", ", group.Select(x => x.HandlerType.Name))}"));

            throw new InvalidOperationException(
                $"Multiple application handlers registered for the same command/query:{Environment.NewLine}{message}");
        }

        foreach (var registration in registrations)
        {
            services.AddScoped(registration.HandlerInterface, registration.HandlerType);
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

