using Microsoft.Extensions.Configuration;

using NG.PlexToDiscord.Exceptions;

using Plex.Api.Factories;
using Plex.Library.ApiModels.Accounts;
using Plex.Library.ApiModels.Servers;

using Serilog;

namespace NG.PlexToDiscord
{
    internal class PlexPollingClient
    {
        private readonly IConfiguration _configuration;
        private readonly IPlexFactory _plexFactory;

        private Timer _timer;
        private bool _disableStart;

        internal PlexPollingClient(IConfiguration configuration, IPlexFactory plexFactory)
        {
            _timer = new Timer((_) => { });
            _configuration = configuration;
            _plexFactory = plexFactory;
            _disableStart = false;
        }

        internal async Task Start()
        {
            if (_disableStart)
            {
                throw new UnrecoverableException("Can't start the PlexPollingClient while another is running!");
            }

            _disableStart = true;

            string authType = _configuration.GetValue<string>(Constants.ConfigurationKeys.AppSettingsJson.PLEX_AUTH_TYPE);

            Log.Information($"Logging into Plex, using: {authType} Auth");
            PlexAccount account = authType.ToLowerInvariant() switch
            {
                "basic" => _plexFactory.GetPlexAccount(
                    _configuration.GetValue<string>(Constants.ConfigurationKeys.SecretsJson.USERNAME),
                    _configuration.GetValue<string>(Constants.ConfigurationKeys.SecretsJson.PASSWORD)
                ),
                "token" => _plexFactory.GetPlexAccount(
                    _configuration.GetValue<string>(Constants.ConfigurationKeys.SecretsJson.TOKEN)
                ),
                _ => throw new UnrecoverableException($"Unknown value for '{Constants.ConfigurationKeys.AppSettingsJson.PLEX_AUTH_TYPE}' in configuration!")
            };

            int pollingRateConfigured = _configuration.GetValue<int>(Constants.ConfigurationKeys.AppSettingsJson.POLLING_RATE_IN_SECONDS);
            int pollingRateInMilliseconds = Convert.ToInt32(TimeSpan.FromSeconds(60).TotalMilliseconds);
            string serverNameToMonitor = _configuration.GetValue<string>(Constants.ConfigurationKeys.AppSettingsJson.SERVER_NAME_TO_MONITOR);

            Log.Information("Selecting server...");
            List<Server> servers = await account.Servers();
            Server monitoredServer = servers.Count switch
            {
                > 1 => servers.First(s => s.FriendlyName == serverNameToMonitor && s.Owned == 1),
                1 => servers.First(s => s.Owned == 1),
                _ => throw new UnrecoverableException("You have no servers on your Plex account!")
            };

            if (monitoredServer == null)
            {
                throw new UnrecoverableException("Something went wrong, couldn't find your server. Check appsettings.json");
            }

            Log.Information($"Server '{monitoredServer.FriendlyName}' selected.");

            bool canStartCheck = true;
            _timer = new(async (state) =>
            {
                if (canStartCheck)
                {
                    canStartCheck = false;
                    Log.Information("Starting Refresh...");
                    await monitoredServer.RefreshSync();

                    // TODO: stuff with the new items here

                    var recentlyAdded = await monitoredServer.HomeHubRecentlyAdded(0, 20);
                    Log.Information("Refresh complete.");
                    canStartCheck = true;
                }
                else
                {
                    Log.Information("Tried to refresh before the previous one ended, skipping.");
                }

            }, state: 0, dueTime: 0, pollingRateInMilliseconds);
        }

        internal void Reset()
        {
            _timer.Dispose();
            _disableStart = false;
        }
    }
}
