using System.Threading;
using Jellyfin.Plugin.Jellydash.Events;
using Jellyfin.Plugin.Jellydash.Services;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net.WebSocketMessages.Outbound;
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
        // Register database helper as a singleton so all consumers share the same instance.
        serviceCollection.AddSingleton(sp =>
        {
            var applicationPaths = sp.GetRequiredService<IApplicationPaths>();
            var helper = new DatabaseHelper(applicationPaths.DataPath);
            helper.Initialize();
            return helper;
        });

        // Register our playback activity tracker so it receives playback start/stop events.
        serviceCollection.AddScoped<IEventConsumer<PlaybackStartEventArgs>, PlaybackTracker>();
        serviceCollection.AddScoped<IEventConsumer<PlaybackProgressEventArgs>, PlaybackTracker>();
        serviceCollection.AddScoped<IEventConsumer<PlaybackStopEventArgs>, PlaybackTracker>();

        // Register activity repository as a singleton service for API controllers.
        serviceCollection.AddSingleton<PlaybackEntryRepository>();
    }
}
