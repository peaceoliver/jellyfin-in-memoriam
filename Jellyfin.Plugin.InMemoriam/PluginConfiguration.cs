using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.InMemoriam.Configuration;

/// <summary>
/// Configuration settings for the In Memoriam plugin.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Number of days to look back for deceased actors (default: 90).
    /// </summary>
    public int LookbackDays { get; set; } = 90;

    /// <summary>
    /// Cached collection ID to improve performance.
    /// </summary>
    public Guid? CollectionId { get; set; }
}