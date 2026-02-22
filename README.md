# Jellyfin In Memoriam Plugin

A Jellyfin plugin that automatically creates a collection of movies featuring recently deceased actors. The plugin uses TheMovieDB metadata to identify actors who have passed away within a configurable timeframe and maintains an "In Memoriam" collection in your Jellyfin library.

## Features

- ✨ Automatically detects recently deceased actors (configurable lookback period)
- 📽️ Creates an "In Memoriam" collection with all their films
- 🎬 Includes actor details and career information in the collection overview
- 📅 Runs on server startup and daily at 2:00 AM (configurable)
- 🗑️ Automatically removes the collection if no matching actors are found
- 📊 Detailed logging for monitoring and debugging

## Installation

### Prerequisites

- Jellyfin 10.10.3 or newer (compatible with .NET 8)
- Note: Will stop working when Jellyfin 11.0 is released (which requires .NET 9)

### Steps

1. Download the latest release from the [Releases](https://github.com/peaceoliver/Jellyfin.Plugin.InMemoriam/releases) page
2. Extract the `.dll` file and any dependencies
3. Place them in your Jellyfin plugins directory:
   - **Linux/Docker**: `/var/lib/jellyfin/plugins/`
   - **Windows**: `%ProgramData%\Jellyfin\Server\plugins\`
   - **Synology**: `/volume1/@appstore/Jellyfin/plugins/`
4. Restart Jellyfin
5. Navigate to Admin > Dashboard > Plugins and enable the "In Memoriam" plugin

### Alternative: Install via Plugin Repository

You can also add this plugin repository directly in Jellyfin for automatic updates:

1. Go to **Admin > Dashboard > Plugins > Repositories**
2. Click the **+** button to add a new repository
3. Enter the repository URL:
   ```
   https://raw.githubusercontent.com/peaceoliver/Jellyfin.Plugin.InMemoriam/main/manifest.json
   ```
4. Click **Add**
5. Go to **Catalog** tab
6. Find "In Memoriam" and click **Install**
7. Restart Jellyfin

## Configuration

Once installed, you can configure the plugin:

1. Go to **Admin > Dashboard > Plugins > In Memoriam**
2. Set the **Lookback Days** (default: 90 days)
   - This determines how far back to search for recently deceased actors
   - Adjust to include older or more recent deaths

The collection will be created automatically when the scheduled task runs.

## How It Works

1. **Startup**: Task runs when Jellyfin server starts
2. **Daily Run**: Task runs daily at 2:00 AM (UTC)
3. **Metadata Check**: Queries all actors in your library against TheMovieDB
4. **Detection**: Identifies actors who passed away within the lookback period
5. **Collection**: Creates or updates the "In Memoriam" collection with their films
6. **Cleanup**: Automatically removes the collection if no matching actors are found

## Scheduled Tasks

You can manually trigger the task from **Admin > Dashboard > Scheduled Tasks**:

1. Find **"Update In Memoriam Collection"**
2. Click **"Run"** to execute immediately

## Troubleshooting

### Collection not appearing

- Check that your actors have death dates in TheMovieDB metadata
- Ensure the lookback period includes the death dates
- Check the Jellyfin logs for errors:
  ```
  grep "In Memoriam" /var/log/jellyfin/*.log
  ```

### Collection appears empty

- This usually means no movies in your library feature the deceased actors
- Check that the actors are properly linked to movies in the metadata

### Performance issues

- The first run builds a cache of all movies and cast members (can take time on large libraries)
- Subsequent runs should be faster

## Development

### Building from Source

```bash
cd Jellyfin.Plugin.InMemoriam
dotnet build -c Release
```

The compiled plugin will be in `bin/Release/net8.0/`

### Project Structure

```
├── Plugin.cs              # Main plugin class
├── InMemoriamTask.cs      # Scheduled task logic
├── PluginConfiguration.cs # Configuration class
├── Configuration/
│   └── configPage.html    # Plugin settings page (optional)
└── Jellyfin.Plugin.InMemoriam.csproj
```

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE) file for details.

## Contributing

Contributions are welcome! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines on how to:

- Report bugs
- Suggest features
- Submit pull requests

## Support

For issues, feature requests, or questions:

1. Check existing [Issues](https://github.com/peaceoliver/Jellyfin.Plugin.InMemoriam/issues)
2. Create a new issue with detailed information about your problem

## Related Projects

- [Jellyfin](https://jellyfin.org/) - The free software media system
- [TheMovieDB](https://www.themoviedb.org/) - The provider of metadata

## Disclaimer

This plugin uses external metadata sources. Accuracy of death dates depends on the quality and timeliness of TheMovieDB data. Please verify important information independently.

---

**Version**: 1.0.0  
**Last Updated**: February 2026  
**Jellyfin Compatibility**: 10.10.3 - 10.x (.NET 8 only)
