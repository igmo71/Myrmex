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

    public static IServiceCollection AddMyrmexAppDispatching(this IServiceCollection services, params Assembly[] handlerAssemblies)
    {
        ArgumentNullException.ThrowIfNull(handlerAssemblies);

        services.AddScoped<ICommandDispatcher, CommandDispatcher>();
        services.AddScoped<IQueryDispatcher, QueryDispatcher>();

        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        services.AddSingleton<IDomainEventHandlerRegistry, DomainEventHandlerRegistry>();

        if (handlerAssemblies.Length == 0)
            return services;

        Type[] handlerTypes = FindHandlerTypes(handlerAssemblies);

        AddApplicationHandlers(services, handlerTypes);
        AddDomainEventHandlers(services, handlerTypes);

        return services;
    }

    private static Type[] FindHandlerTypes(Assembly[] handlerAssemblies)
    {
        return handlerAssemblies
            .Distinct()
            .SelectMany(assembly => assembly.DefinedTypes)
            .Where(type =>
                type is { IsAbstract: false, IsInterface: false } &&
                type.ImplementedInterfaces.Any(IsSupportedHandlerInterface))
            .Select(type => type.AsType())
            .Distinct()
            .ToArray();
    }

    private static bool IsSupportedHandlerInterface(Type type)
    {
        return IsApplicationHandlerInterface(type) || IsDomainEventHandlerInterface(type);
    }

    private static bool IsApplicationHandlerInterface(Type type)
    {
        if (!type.IsGenericType)
            return false;

        Type genericTypeDefinition = type.GetGenericTypeDefinition();

        return genericTypeDefinition == typeof(ICommandHandler<,>) || genericTypeDefinition == typeof(IQueryHandler<,>);
    }

    private static bool IsDomainEventHandlerInterface(Type type)
    {
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IDomainEventHandler<>);
    }

    private static IServiceCollection AddApplicationHandlers(IServiceCollection services, IReadOnlyCollection<Type> handlerTypes)
    {
        var registrations = handlerTypes
            .SelectMany(handlerType => handlerType
                .GetInterfaces()
                .Where(IsApplicationHandlerInterface)
                .Distinct()
                .Select(handlerInterface => new HandlerRegistration(handlerInterface, handlerType)))
            .ToArray();

        var duplicates = registrations
            .GroupBy(registration => registration.HandlerInterface)
            .Where(group => group.Count() > 1)
            .ToArray();

        if (duplicates.Length > 0)
        {
            string message = string.Join(Environment.NewLine,
                duplicates.Select(group => $"{group.Key}: {string.Join(", ", group.Select(x => x.HandlerType.Name))}"));

            throw new InvalidOperationException(
                $"Multiple application handlers registered for the same command/query:{Environment.NewLine}{message}");
        }

        foreach (var registration in registrations)
        {
            services.AddScoped(registration.HandlerInterface, registration.HandlerType);
        }

        return services;
    }

    private static IServiceCollection AddDomainEventHandlers(IServiceCollection services, IReadOnlyCollection<Type> handlerTypes)
    {
        foreach (Type handlerType in handlerTypes)
        {
            Type[] eventTypes = handlerType
                .GetInterfaces()
                .Where(IsDomainEventHandlerInterface)
                .Select(interfaceType => interfaceType.GetGenericArguments()[0])
                .Distinct()
                .ToArray();

            if (eventTypes.Length == 0)
                continue;

            services.AddScoped(handlerType);

            foreach (Type eventType in eventTypes)
            {
                services.AddSingleton(new DomainEventHandlerDescriptor(eventType, handlerType));
            }
        }

        return services;
    }

    private sealed record HandlerRegistration(Type HandlerInterface, Type HandlerType);
}