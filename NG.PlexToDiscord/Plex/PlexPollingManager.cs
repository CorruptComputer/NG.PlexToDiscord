using Microsoft.Extensions.Configuration;

using NG.PlexToDiscord.Constants;
using NG.PlexToDiscord.Exceptions;

using Plex.Api.Factories;
using Plex.Library.ApiModels.Accounts;
using Plex.Library.ApiModels.Servers;

using Serilog;

namespace NG.PlexToDiscord.Plex;

internal class PlexPollingManager
{
    private readonly IConfiguration _configuration;
    private readonly IPlexFactory _plexFactory;

    private Timer _timer;
    private bool _disableStart;
    private PlexAccount? _account;
    private Server? _monitoredServer;
    private int _pollingRate;

    internal PlexPollingManager(IConfiguration configuration, IPlexFactory plexFactory)
    {
        _configuration = configuration;
        _plexFactory = plexFactory;

        _timer = new Timer((_) => { });
        _disableStart = false;
        _account = null;
        _monitoredServer = null;
        _pollingRate = 0;
    }

    internal async Task StartPolling()
    {
        // Basic safety check to make sure we don't accidentally start more than 1 timer.
        if (_disableStart)
        {
            throw new UnrecoverableException("Can't start the PlexPollingClient while another is running!");
        }

        _disableStart = true;

        // Login to Plex
        Login();

        // Get some values from the config before we start
        ConfigurePollingRate();

        // Select a server to monitor
        await SelectServer().ConfigureAwait(false);

        // Finally start the timer to check this server periodically for updates
        PlexPollingClient plexPollingClient = new(_account, _monitoredServer);
        _timer = new(async (state) =>
        {
            await plexPollingClient.PollForUpdatedLibraries().ConfigureAwait(false);
        }, state: 0, dueTime: 0, _pollingRate);
    }

    internal void ResetPolling()
    {
        // This should handle the cancellation of any threads in progress, as well as clearing the timer so it does not start again.
        _timer.Dispose();
        _disableStart = false;
    }

    private void Login()
    {
        string authType = _configuration.GetValue<string>(ConfigurationKeys.AppSettingsJson.PLEX_AUTH_TYPE);
        Log.Information($"Logging into Plex, using auth type: {authType}");

        _account = authType.ToLowerInvariant() switch
        {
            "basic" => _plexFactory.GetPlexAccount(
                _configuration.GetValue<string>(ConfigurationKeys.SecretsJson.USERNAME),
                _configuration.GetValue<string>(ConfigurationKeys.SecretsJson.PASSWORD)
            ),
            "token" => _plexFactory.GetPlexAccount(
                _configuration.GetValue<string>(ConfigurationKeys.SecretsJson.TOKEN)
            ),
            _ => throw new UnrecoverableException($"Unknown value for '{ConfigurationKeys.AppSettingsJson.PLEX_AUTH_TYPE}' in configuration!")
        };

        if (_account == null)
        {
            throw new UnrecoverableException("Logging into Plex failed.");
        }
    }

    private async Task SelectServer()
    {
        if (_account == null)
        {
            return;
        }

        string serverNameToMonitor = _configuration.GetValue<string>(ConfigurationKeys.AppSettingsJson.SERVER_NAME_TO_MONITOR);

        Log.Information("Selecting server...");
        List<Server> ownedServers = (await _account.Servers().ConfigureAwait(false)).Where(s => s.Owned == 1).ToList();
        if (ownedServers.Any())
        {
            if (ownedServers.Count > 1)
            {
                Log.Information("Multiple owned servers found, using 'appsettings.json' name value to select.");
                _monitoredServer = ownedServers.FirstOrDefault(s => s?.FriendlyName == serverNameToMonitor, null);
            }
            else
            {
                Log.Information("Only one owned server found.");
                _monitoredServer = ownedServers.First();
            }
        }

        if (_monitoredServer == null)
        {
            throw new UnrecoverableException("Unable to select a server, either you have none owned or the name in appsettings.json is incorrect.");
        }

        Log.Information($"Server '{_monitoredServer.FriendlyName}' selected.");
    }

    private void ConfigurePollingRate()
    {
        int pollingRateConfigured = _configuration.GetValue<int>(ConfigurationKeys.AppSettingsJson.POLLING_RATE_IN_SECONDS);

        Log.Information($"Polling every {pollingRateConfigured} seconds.");

        _pollingRate = Convert.ToInt32(TimeSpan.FromSeconds(pollingRateConfigured).TotalMilliseconds);
    }
}
