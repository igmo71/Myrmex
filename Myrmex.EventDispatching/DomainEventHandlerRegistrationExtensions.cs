using Microsoft.Extensions.DependencyInjection;
using Myrmex.Core.Events;
using System.Reflection;

namespace Myrmex.EventDispatching;

public static class DomainEventHandlerRegistrationExtensions
{
    public static IServiceCollection AddDomainEventHandlersFromAssemblies(this IServiceCollection services, params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(assemblies);

        Type openHandlerType = typeof(IDomainEventHandler<>);

        IEnumerable<Type> handlerTypes = assemblies
            .SelectMany(assembly => assembly.DefinedTypes)
            .Where(type =>
                type is { IsAbstract: false, IsInterface: false } &&
                type.ImplementedInterfaces.Any(t => t.IsGenericType && t.GetGenericTypeDefinition() == openHandlerType))
            .Select(type => type.AsType())
            .Distinct();

        foreach (Type handlerType in handlerTypes)
        {
            Type[] eventTypes = handlerType
                .GetInterfaces()
                .Where(t => t.IsGenericType && t.GetGenericTypeDefinition() == openHandlerType)
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
}