using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using NG.PlexToDiscord.Exceptions;
using NG.PlexToDiscord.Plex;

using Plex.Api.Factories;
using Plex.Library.Factories;
using Plex.ServerApi;
using Plex.ServerApi.Api;
using Plex.ServerApi.Clients;
using Plex.ServerApi.Clients.Interfaces;

using Serilog;

namespace NG.PlexToDiscord;

public static class Program
{
    public static async Task Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration().Enrich.FromLogContext().WriteTo.Console().CreateLogger();
        try
        {
            Log.Information($"Starting up...");
            using IHost host = Setup(args);
            Log.Information($"Setup complete.");

            IConfiguration configuration = host.Services.GetService<IConfiguration>()
                ?? throw new UnrecoverableException($"{nameof(IConfiguration)} resolves to null!");

            IPlexFactory plexFactory = host.Services.GetService<IPlexFactory>()
                ?? throw new UnrecoverableException($"{nameof(IPlexFactory)} resolves to null!");

            PlexPollingManager plexPollingManager = new(configuration, plexFactory);
            bool shouldTryAgain = false;
            do
            {
                try
                {
                    await plexPollingManager.StartPolling().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    if (ex is RecoverableException)
                    {
                        Log.Warning($"Recoverable error occoured: {ex.Message}");
                        Log.Warning("Resetting...");
                        plexPollingManager.ResetPolling();
                        shouldTryAgain = true;
                    }
                    else
                    {
                        Log.Error($"Unrecoverable error occoured: {ex.Message}");
                        Log.Error("Exiting...");
                        shouldTryAgain = false;
                        await host.StopAsync();
                    }
                }
            } while (shouldTryAgain);

            await host.WaitForShutdownAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Error(ex.Message);
        }
    }

    private static IHost Setup(string[] args)
    {
        IHostBuilder builder = Host.CreateDefaultBuilder(args);

        // Load our configuration
        builder.ConfigureAppConfiguration((hostingContext, configuration) =>
        {
            configuration.AddJsonFile("./Configs/appsettings.json", optional: false, reloadOnChange: false)
                         .AddJsonFile("./Configs/secrets.json", optional: false, reloadOnChange: false);
        });

        // Setup logging
        builder.ConfigureLogging(logging =>
        {
            logging.AddSerilog(dispose: true);
        });

        // Setup dependancy injection
        builder.ConfigureServices((hostingContext, services) =>
        {
            ClientOptions apiOptions = new();
            hostingContext.Configuration.GetSection(nameof(ClientOptions)).Bind(apiOptions);

            services.AddSingleton(apiOptions);
            services.AddTransient<IPlexServerClient, PlexServerClient>();
            services.AddTransient<IPlexAccountClient, PlexAccountClient>();
            services.AddTransient<IPlexLibraryClient, PlexLibraryClient>();
            services.AddTransient<IApiService, ApiService>();
            services.AddTransient<IPlexFactory, PlexFactory>();
            services.AddTransient<IPlexRequestsHttpClient, PlexRequestsHttpClient>();
        });

        builder.UseConsoleLifetime();

        return builder.Build();
    }
}
