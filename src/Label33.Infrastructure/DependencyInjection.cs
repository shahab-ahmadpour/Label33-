using Label33.Application.Abstractions;
using Label33.Infrastructure.Identity;
using Label33.Infrastructure.Payments;
using Label33.Infrastructure.Persistence;
using Label33.Infrastructure.Storage;
using Label33.Infrastructure.Time;
using Label33.Infrastructure.Workers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Label33.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddLabel33Infrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration.GetValue<string>("Database:Provider") ?? "SqlServer";
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Server=(localdb)\\mssqllocaldb;Database=Label33Db;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

        services.AddDbContext<AppDbContext>(options =>
        {
            if (string.Equals(provider, "Sqlite", StringComparison.OrdinalIgnoreCase))
                options.UseSqlite(connectionString);
            else
                options.UseSqlServer(connectionString);
        });

        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        services
            .AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
            {
                options.Password.RequiredLength = 6;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<IOrderNumberGenerator, OrderNumberGenerator>();
        services.AddScoped<IPaymentGateway, MockPaymentGateway>();

        var emailRoot = configuration.GetValue<string>("Email:FileRoot")
            ?? Path.Combine(AppContext.BaseDirectory, "App_Data", "email");
        services.AddSingleton<IEmailSender>(sp =>
            new FileEmailSender(emailRoot, sp.GetRequiredService<ILogger<FileEmailSender>>()));

        var storageRoot = configuration.GetValue<string>("Storage:Root")
            ?? Path.Combine(AppContext.BaseDirectory, "App_Data", "files");
        services.AddSingleton<IFileStorage>(_ => new LocalFileStorage(storageRoot));

        services.AddHostedService<ReservationCleanupWorker>();

        return services;
    }
}
