using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using NG.PlexToDiscord.Exceptions;

using Plex.Api.Factories;
using Plex.Library.Factories;
using Plex.ServerApi;
using Plex.ServerApi.Api;
using Plex.ServerApi.Clients;
using Plex.ServerApi.Clients.Interfaces;

namespace NG.PlexToDiscord;

public static class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine($"Starting up...");
        using IHost host = Setup(args);
        Console.WriteLine($"Setup complete.");

        IConfiguration configuration = host.Services.GetService<IConfiguration>() 
            ?? throw new UnrecoverableException($"{nameof(IConfiguration)} resolves to null!");

        IPlexFactory plexFactory = host.Services.GetService<IPlexFactory>()
            ?? throw new UnrecoverableException($"{nameof(IPlexFactory)} resolves to null!");


        PlexPollingClient plexPollingClient = new(configuration, plexFactory);
        bool shouldTryAgain = false;
        do
        {
            try
            {
                await plexPollingClient.Start();
            }
            catch (Exception ex)
            {
                if (ex is RecoverableException)
                {
                    Console.WriteLine($"Recoverable error occoured: {ex.Message}");
                    Console.WriteLine("Resetting...");
                    plexPollingClient.Reset();
                    shouldTryAgain = true;
                }
                else
                {
                    Console.WriteLine($"Unrecoverable error occoured: {ex.Message}");
                    Console.WriteLine("Exiting...");
                    plexPollingClient.Reset();
                    shouldTryAgain = false;
                    host.Dispose();
                }
            }
        } while (shouldTryAgain);
    }

    private static IHost Setup(string[] args)
    {
        IHostBuilder builder = Host.CreateDefaultBuilder(args);

        // Load our configuration
        builder.ConfigureAppConfiguration((hostingContext, configuration) =>
        {
            configuration.AddJsonFile("./Configs/appsettings.json", optional: false, reloadOnChange: false)
                         .AddJsonFile("./Configs/secrets.json", optional: false, reloadOnChange: false);
        }
        );

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
        }
        );

        builder.UseConsoleLifetime();

        return builder.Build();
    }

    private static bool CheckForNewContent()
    {
        return false;
    }
}
