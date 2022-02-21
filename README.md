# Plex to Discord

## Built using 
- [Plex.Api](https://github.com/jensenkd/plex-api)
- [Serilog](https://github.com/serilog/serilog)
  - [Serilog.Extensions.Logging](https://github.com/serilog/serilog-extensions-logging)
  - [Serilog.Sinks.Console](https://github.com/serilog/serilog-sinks-console)

## Notes

Poll Plex for new media 

If found send a message to the Discord channel with a list

Something like should be good I think.
```
{show.Name}: Season {show.Season} Episode {show.Episode} is available on {server.FriendlyName}.
{show.Name}: Season {show.Season} Episode {show.Episode} is available on {server.FriendlyName}.
{movie.Name}: Movie is available on {server.FriendlyName}.
```

Stretch goal: Messages for removed also.
```
{show.Name}: Season {show.Season} Episode {show.Episode} has been removed from {server.FriendlyName}.
{show.Name}: Season {show.Season} Episode {show.Episode} is available on {server.FriendlyName}.
{movie.Name}: Movie is available on {server.FriendlyName}.
{movie.Name}: Movie has been removed from {server.FriendlyName}.
```