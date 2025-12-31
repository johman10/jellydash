using Jellyfin.Plugin.Jellydash.Events;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Jellydash.Services;

/// <summary>
/// Registers Jellydash plugin services with the Jellyfin DI container.
/// </summary>
public sealed class JellydashServiceRegistrator : IPluginServiceRegistrator
{
    /// <inheritdoc />
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        // Register our playback history logger so it receives playback start/stop events.
        serviceCollection.AddScoped<IEventConsumer<PlaybackStartEventArgs>, PlaybackHistoryLogger>();
        serviceCollection.AddScoped<IEventConsumer<PlaybackStopEventArgs>, PlaybackHistoryLogger>();
    }
}
