
using Plex.Library.ApiModels.Accounts;
using Plex.Library.ApiModels.Libraries;
using Plex.Library.ApiModels.Servers;

using Serilog;

namespace NG.PlexToDiscord.Plex
{
    internal class PlexPollingClient
    {
        private bool _canStartCheck;
        private PlexAccount _plexAccount;
        private Server _monitoredServer;

        internal PlexPollingClient(PlexAccount account, Server monitoredServer)
        {
            _plexAccount = account;
            _monitoredServer = monitoredServer;

            _canStartCheck = true;
        }

        internal async Task PollForUpdatedLibraries()
        {
            // Basic safety check to make sure we don't accidentally start more than 1 check.
            if (!_canStartCheck)
            {
                Log.Information("Tried to refresh before the previous one ended, skipping.");
                return;
            }

            _canStartCheck = false;


            Log.Information("Starting Refresh...");
            await _monitoredServer.RefreshSync().ConfigureAwait(false);
            Log.Information("Refresh complete.");


            Log.Information("Checking Libraries for updates...");
            List<LibraryBase> libraries = await _monitoredServer.Libraries().ConfigureAwait(false);
            foreach (LibraryBase library in libraries)
            {
                Log.Information($"Found library: {library.Title}");

                // TODO: This is broken. Why?
                //MediaContainer recentlyAdded = await library.RecentlyAdded();
            }

            _canStartCheck = true;
        }
    }
}
