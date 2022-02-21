namespace NG.PlexToDiscord.Constants;

internal static class ConfigurationKeys
{
    // appsettings.json
    internal static class AppSettingsJson
    {
        internal const string CLIENT_OPTIONS = "ClientOptions";

        // Honestly I'm not sure this would ever be needed, might remove this at some point.
        internal static class ClientOptions
        {
            internal const string PRODUCT = "Product";
            internal const string DEVICE_NAME = "DeviceName";
            internal const string CLIENT_ID = "ClientId";
            internal const string PLATFORM = "Platform";
            internal const string VERSION = "Version";
        }

        internal const string PLEX_AUTH_TYPE = "PlexAuthType";
        internal const string POLLING_RATE_IN_SECONDS = "PollingRateInSeconds";
        internal const string SERVER_NAME_TO_MONITOR = "ServerNameToMonitor";
    }


    // secrets.json
    internal static class SecretsJson
    {
        internal const string USERNAME = "Username";
        internal const string PASSWORD = "Password";
        internal const string TOKEN = "Token";
    }
}
