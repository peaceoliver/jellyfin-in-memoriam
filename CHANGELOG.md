# Changelog

All notable changes to the Jellyfin In Memoriam Plugin project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2026-02-22

### Added
- Initial release of the In Memoriam plugin
- Automatic detection of recently deceased actors based on configurable lookback period
- Creates "In Memoriam" collection of movies featuring deceased actors
- Scheduled task runs on server startup and daily at 2:00 AM
- Configurable lookback period (default: 90 days)
- Collection metadata including actor information and career details
- Automatic cleanup: removes collection if no matching actors found
- Fallback name-based actor matching when metadata is incomplete
- Comprehensive logging for troubleshooting
- Full XML documentation for code clarity
- MIT License

### Technical Details
- Built with .NET 8.0
- Compatible with Jellyfin 10.10.3 and newer 10.x versions
- Uses Jellyfin Controller and Model packages
- Implements IScheduledTask interface

---

## Future Versions

### Planned Features
- Support for additional person types (directors, writers, etc.)
- Customizable collection name
- Multiple lookback date collections
- Integration with Jellyfin notifications
- Web dashboard for statistics

### Known Limitations
- Will not work with Jellyfin 11.0+ (requires .NET 9 update)
- Depends on TheMovieDB data accuracy for death dates

---

For more information about releases, please visit the [GitHub Releases](https://github.com/peaceoliver/Jellyfin.Plugin.InMemoriam/releases) page.
