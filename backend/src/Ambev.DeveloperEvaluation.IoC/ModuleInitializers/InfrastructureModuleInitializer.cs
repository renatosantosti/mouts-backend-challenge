using Ambev.DeveloperEvaluation.Application.Sales;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.ORM;
using Ambev.DeveloperEvaluation.ORM.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Ambev.DeveloperEvaluation.IoC.ModuleInitializers;

public class InfrastructureModuleInitializer : IModuleInitializer
{
    public void Initialize(WebApplicationBuilder builder)
    {
        builder.Services.Configure<MongoSettings>(builder.Configuration.GetSection(MongoSettings.SectionName));
        builder.Services.AddScoped<DbContext>(provider => provider.GetRequiredService<DefaultContext>());
        builder.Services.AddScoped<IUserRepository, UserRepository>();
        builder.Services.AddScoped<ISaleRepository, SaleRepository>();
        builder.Services.AddScoped<ISaleEventPublisher, SimulatedSalesEventBroker>();
        builder.Services.AddSingleton<MongoSaleEventHistoryRepository>();
        builder.Services.AddScoped<ISaleEventHistoryWriter>(provider => provider.GetRequiredService<MongoSaleEventHistoryRepository>());
        builder.Services.AddScoped<ISaleEventHistoryReader>(provider => provider.GetRequiredService<MongoSaleEventHistoryRepository>());
        builder.Services.AddScoped<ISaleEventHistoryRecorder, SaleEventHistoryRecorder>();
    }
}