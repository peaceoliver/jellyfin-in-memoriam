/// <summary>
/// This file contains the main Plugin class for the "In Memoriam" Jellyfin plugin.
/// The In Memoriam plugin automatically creates a collection of movies featuring recently deceased actors,
/// pulling metadata from TheMovieDB. It runs on server startup and daily to keep the collection current.
/// </summary>
/// <remarks>
/// License: MIT
/// Repository: https://github.com/peaceoliver/jellyfin-in-memoriam
/// Contributors: Feel free to fork, use, and submit pull requests to improve this plugin!
/// For more information, see the README.md and CONTRIBUTING.md files.
/// </remarks>

using System;
using System.Collections.Generic;
using Jellyfin.Plugin.InMemoriam.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Controller.Plugins;

namespace Jellyfin.Plugin.InMemoriam;

/// <summary>
/// The main Plugin class for the "In Memoriam" Jellyfin plugin.
/// This class manages the plugin's configuration, metadata, and web pages.
/// </summary>
public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    /// <summary>
    /// Gets the display name of the plugin.
    /// </summary>
    public override string Name => "In Memoriam";

    /// <summary>
    /// Gets the unique identifier (GUID) for this plugin.
    /// This GUID is used to distinguish this plugin from others in Jellyfin.
    /// </summary>
    public override Guid Id => Guid.Parse("30ae6b4c-622f-4d77-aa37-97dd638d78d0"); // Use your actual GUID

    /// <summary>
    /// Gets the singleton instance of the Plugin class.
    /// The null-forgiving operator (!) tells the compiler that this will be initialized 
    /// as soon as the plugin starts, preventing null reference warnings.
    /// </summary>
    public static Plugin Instance { get; private set; } = null!;

    /// <summary>
    /// Initializes a new instance of the Plugin class.
    /// </summary>
    /// <param name="applicationPaths">Provides paths and configuration directories for the plugin.</param>
    /// <param name="xmlSerializer">Used to serialize and deserialize the plugin configuration.</param>
    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
    }

    /// <summary>
    /// Gets the collection of web pages provided by this plugin.
    /// These pages are accessible from the Jellyfin dashboard.
    /// </summary>
    /// <returns>An enumerable collection of PluginPageInfo objects representing the plugin's web pages.</returns>
    public IEnumerable<PluginPageInfo> GetPages()
    {
        return new[]
        {
            new PluginPageInfo
            {
                Name = "InMemoriam",
                EmbeddedResourcePath = GetType().Namespace + ".Configuration.configPage.html"
            }
        };
    }
}