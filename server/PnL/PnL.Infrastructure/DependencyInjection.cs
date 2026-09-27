using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PnL.Application.Interfaces;
using PnL.Infrastructure.Adapters.Messaging;
using PnL.Infrastructure.Helpers;
using PnL.Infrastructure.Persistence;

namespace PnL.Infrastructure;

/// <summary>
/// Provides extension methods for registering PnL infrastructure services.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddPnLInfrastructure(
        this IServiceCollection services,
        string connectionString,
        RabbitMqOptions rabbitMqOptions)
    {
        services.AddDbContext<PnLDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<IPnLRepository, PnLRepository>();
        services.AddSingleton(rabbitMqOptions);
        services.AddSingleton<RabbitMqConnectionProvider>();
        services.AddSingleton<IFeedPublisher, RabbitMqFeedPublisher>();
        services.AddSingleton<INotificationPublisher, RabbitMqNotificationPublisher>();
        services.AddSingleton<ICsvFeedParser, CsvFeedParser>();

        return services;
    }
}
