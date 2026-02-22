/// <summary>
/// This file contains the InMemoriamTask class, which is a scheduled task for the "In Memoriam" plugin.
/// The task automatically updates a collection of movies featuring recently deceased actors.
/// It queries external metadata sources to identify deceased individuals and maintains a collection accordingly.
/// </summary>
/// <remarks>
/// How it works:
/// 1. Gets all actors from the library
/// 2. Checks external data (TheMovieDb) for death dates within the configured lookback period
/// 3. Finds all movies featuring these deceased actors
/// 4. Creates or updates the "In Memoriam" collection with these movies
/// 5. Deletes the collection if no matching movies are found
/// </remarks>

using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Model.Tasks;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using Jellyfin.Data.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.InMemoriam;

/// <summary>
/// A scheduled task that maintains an "In Memoriam" collection of movies featuring recently deceased actors.
/// Runs on server startup and daily at 2:00 AM to check for deaths and update the collection.
/// </summary>
public class InMemoriamTask : IScheduledTask
{
    private readonly ILibraryManager _libraryManager;
    private readonly ICollectionManager _collectionManager;
    private readonly ILogger<InMemoriamTask> _logger;

    /// <summary>
    /// Initializes a new instance of the InMemoriamTask class.
    /// </summary>
    /// <param name="libraryManager">Manages access to the Jellyfin media library.</param>
    /// <param name="collectionManager">Manages movie collections in Jellyfin.</param>
    /// <param name="logger">Used for logging information and warnings during task execution.</param>
    public InMemoriamTask(ILibraryManager libraryManager, ICollectionManager collectionManager, ILogger<InMemoriamTask> logger)
    {
        _libraryManager = libraryManager;
        _collectionManager = collectionManager;
        _logger = logger;
    }

    /// <summary>
    /// Gets the user-friendly name of this scheduled task.
    /// </summary>
    public string Name => "Update In Memoriam Collection";

    /// <summary>
    /// Gets the unique key identifier for this scheduled task.
    /// Used internally by Jellyfin to reference this specific task.
    /// </summary>
    public string Key => "InMemoriamUpdateTask";

    /// <summary>
    /// Gets the description of this scheduled task.
    /// Displayed in the Jellyfin task scheduler interface.
    /// </summary>
    public string Description => "Updates recently deceased actors. Deletes the collection if no matches are found.";

    /// <summary>
    /// Gets the category this task belongs to in the Jellyfin task scheduler.
    /// </summary>
    public string Category => "Maintenance";

    /// <summary>
    /// Executes the In Memoriam task asynchronously.
    /// This method runs the main logic to check for deceased actors and update the collection.
    /// </summary>
    /// <param name="progress">Reports progress of the task execution (0-100%).</param>
    /// <param name="cancellationToken">Used to cancel the task if requested by Jellyfin.</param>
    /// <returns>A Task representing the asynchronous operation.</returns>
    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        var config = Plugin.Instance.Configuration;
        var days = config.LookbackDays;
        var lookbackPeriod = DateTime.UtcNow.AddDays(-days);
        var collectionName = "In Memoriam";

        Dictionary<string, List<Movie>>? moviesByPersonName = null;

        /// <summary>
        /// Local helper function to build an index of movies by person name.
        /// This is used as a fallback when external person data is not available.
        /// </summary>
        List<Movie> GetMoviesByPersonName(string personName)
        {
            if (moviesByPersonName == null)
            {
                _logger.LogWarning("In Memoriam: Building name-based index for cast fallback.");
                moviesByPersonName = new Dictionary<string, List<Movie>>(StringComparer.OrdinalIgnoreCase);

                var allMovies = _libraryManager.GetItemList(new InternalItemsQuery
                {
                    IncludeItemTypes = new[] { BaseItemKind.Movie },
                    Recursive = true
                }).OfType<Movie>();

                foreach (var movie in allMovies)
                {
                    var credits = _libraryManager.GetPeople(movie);
                    foreach (var credit in credits)
                    {
                        if (string.IsNullOrWhiteSpace(credit.Name))
                        {
                            continue;
                        }

                        if (!moviesByPersonName.TryGetValue(credit.Name, out var list))
                        {
                            list = new List<Movie>();
                            moviesByPersonName[credit.Name] = list;
                        }

                        list.Add(movie);
                    }
                }
            }

            return moviesByPersonName.TryGetValue(personName, out var result)
                ? result
                : new List<Movie>();
        }

        _logger.LogWarning("In Memoriam: Checking for deaths after {Date} ({Days} day lookback)", lookbackPeriod.ToShortDateString(), days);

        // Find existing collection
        BoxSet? collection = null;
        if (config.CollectionId.HasValue)
        {
            collection = _libraryManager.GetItemById(config.CollectionId.Value) as BoxSet;
            if (collection == null)
            {
                _logger.LogWarning("In Memoriam: Stored collection id {Id} not found. Falling back to name lookup.", config.CollectionId.Value);
            }
        }

        var collectionsByName = _libraryManager.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = new[] { BaseItemKind.BoxSet },
            Name = collectionName
        }).OfType<BoxSet>().ToList();

        if (collection == null && collectionsByName.Count > 0)
        {
            if (collectionsByName.Count > 1)
            {
                _logger.LogWarning("In Memoriam: Found {Count} collections named '{Name}'. Using the newest.", collectionsByName.Count, collectionName);
            }

            collection = collectionsByName
                .OrderByDescending(c => c.DateCreated)
                .FirstOrDefault();

            if (collection != null)
            {
                config.CollectionId = collection.Id;
                Plugin.Instance.SaveConfiguration();
                _logger.LogWarning("In Memoriam: Stored collection id set to {Id}.", collection.Id);
            }
        }

        // Search for deceased actors
        var allPeople = _libraryManager.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = new[] { BaseItemKind.Person },
            Recursive = true
        });

        var deceasedPeople = allPeople
            .Where(p => p.EndDate.HasValue && p.EndDate.Value.Date >= lookbackPeriod.Date)
            .OrderByDescending(p => p.EndDate)
            .ToList();

        _logger.LogWarning("In Memoriam: Found {Count} deceased actors in range.", deceasedPeople.Count);
        foreach (var person in deceasedPeople)
        {
            _logger.LogWarning("In Memoriam: Actor in range: {Name} (EndDate: {EndDate})", person.Name, person.EndDate?.ToString("yyyy-MM-dd"));
        }

        // Auto-Delete Logic
        if (!deceasedPeople.Any())
        {
            if (collection != null)
            {
                _logger.LogWarning("In Memoriam: No actors found in window. Deleting empty collection.");
                
                _libraryManager.DeleteItem(collection, new DeleteOptions 
                { 
                    DeleteFileLocation = false 
                });

                config.CollectionId = null;
                Plugin.Instance.SaveConfiguration();

                _logger.LogWarning("In Memoriam: Collection deleted. You may need to refresh your browser page to see the change.");
            }
            return;
        }

        var movieIds = new List<Guid>();
        var summaryLines = new List<string>();

        foreach (var person in deceasedPeople)
        {
            var deathDate = person.EndDate!.Value.ToString("MMMM dd, yyyy");
            var deathYear = person.EndDate.Value.Year;
            
            var movies = _libraryManager.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = new[] { BaseItemKind.Movie },
                PersonIds = new[] { person.Id },
                Recursive = true
            }).OfType<Movie>().ToList();
            
            if (!movies.Any())
            {
                var nameMatchedMovies = GetMoviesByPersonName(person.Name);
                if (nameMatchedMovies.Any())
                {
                    _logger.LogWarning("In Memoriam: Using name-based fallback for {Name} ({Count} movies).", person.Name, nameMatchedMovies.Count);
                    movies = nameMatchedMovies;
                }
            }

            var personMovieIds = movies.Select(m => m.Id).ToList();
            var movieCount = personMovieIds.Count;
            movieIds.AddRange(personMovieIds);
            
            // Build first line: name, birth-death years, age, death date
            var line1Parts = new List<string>();
            line1Parts.Add($"• {person.Name}");
            
            if (person.PremiereDate.HasValue)
            {
                var birthYear = person.PremiereDate.Value.Year;
                var age = deathYear - birthYear;
                line1Parts.Add($"({birthYear}-{deathYear}, aged {age})");
            }
            else
            {
                line1Parts.Add($"({deathYear})");
            }
            
            line1Parts.Add($"passed away {deathDate}");
            
            // Build second line: movie count, career span, top-rated movie
            var line2Parts = new List<string>();
            line2Parts.Add($"{movieCount} {(movieCount == 1 ? "film" : "films")} in your library");
            
            if (movies.Any())
            {
                var movieYears = movies
                    .Where(m => m.ProductionYear.HasValue)
                    .Select(m => m.ProductionYear!.Value)
                    .OrderBy(y => y)
                    .ToList();
                
                if (movieYears.Any())
                {
                    var firstYear = movieYears.First();
                    var lastYear = movieYears.Last();
                    line2Parts.Add($"Career: {firstYear}-{lastYear}");
                }
                
                var topMovie = movies
                    .Where(m => m.CommunityRating.HasValue)
                    .OrderByDescending(m => m.CommunityRating)
                    .FirstOrDefault();
                
                if (topMovie != null && topMovie.CommunityRating.HasValue)
                {
                    line2Parts.Add($"Top-rated: {topMovie.Name} ({topMovie.CommunityRating.Value:F1}★)");
                }
            }
            
            summaryLines.Add(string.Join(" - ", line1Parts) + " - " + string.Join(" | ", line2Parts));
            
            _logger.LogWarning("In Memoriam: {Name} contributed {MovieCount} movies.", person.Name, personMovieIds.Count);
        }

        if (movieIds.Any())
        {
            var distinctMovieIds = movieIds.Distinct().ToList();
            _logger.LogWarning("In Memoriam: Total distinct movies to add: {MovieCount}", distinctMovieIds.Count);

            // Delete existing collection and recreate (more reliable than add/remove)
            if (collection != null)
            {
                _logger.LogWarning("In Memoriam: Deleting existing collection to recreate with updated items.");
                _libraryManager.DeleteItem(collection, new DeleteOptions 
                { 
                    DeleteFileLocation = false 
                });
                collection = null;
            }

            _logger.LogWarning("In Memoriam: Creating new collection with {MovieCount} movies.", distinctMovieIds.Count);
            await _collectionManager.CreateCollectionAsync(new CollectionCreationOptions
            {
                Name = collectionName,
                ItemIdList = distinctMovieIds.Select(id => id.ToString()).ToArray()
            });

            // Wait for Jellyfin to process the collection creation
            await Task.Delay(500, cancellationToken);

            collection = _libraryManager.GetItemList(new InternalItemsQuery 
            { 
                IncludeItemTypes = new[] { BaseItemKind.BoxSet }, 
                Name = collectionName 
            }).OfType<BoxSet>().OrderByDescending(c => c.DateCreated).FirstOrDefault();

            if (collection != null)
            {
                config.CollectionId = collection.Id;
                Plugin.Instance.SaveConfiguration();
                _logger.LogWarning("In Memoriam: Collection created with id {Id}.", collection.Id);
                
                var actualChildCount = _libraryManager.GetItemList(new InternalItemsQuery
                {
                    ParentId = collection.Id
                }).Count;
                _logger.LogWarning("In Memoriam: Collection now has {Count} items.", actualChildCount);
                
                // Set metadata on the new collection
                collection.Overview = $"Honoring the stars we've lost in the past {days} days:\n\n\n" + string.Join("\n\n", summaryLines);
                
                var spotlightActor = deceasedPeople.First();

                // Set Primary Image (Portrait)
                if (spotlightActor.HasImage(ImageType.Primary))
                {
                    collection.SetImagePath(ImageType.Primary, spotlightActor.GetImagePath(ImageType.Primary));
                }

                // Set Backdrop Image (Wide/Banner)
                if (spotlightActor.HasImage(ImageType.Backdrop))
                {
                    collection.SetImagePath(ImageType.Backdrop, spotlightActor.GetImagePath(ImageType.Backdrop));
                }
                else if (movieIds.Any())
                {
                    var firstMovie = _libraryManager.GetItemById(movieIds.First());
                    if (firstMovie != null && firstMovie.HasImage(ImageType.Backdrop))
                    {
                        collection.SetImagePath(ImageType.Backdrop, firstMovie.GetImagePath(ImageType.Backdrop));
                    }
                }

                await collection.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, cancellationToken);
                _logger.LogWarning("In Memoriam: Task finished. Collection contains {0} actors.", deceasedPeople.Count);
            }
        }
    }

    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers() => 
        new[]
        {
            new TaskTriggerInfo { Type = TaskTriggerInfo.TriggerStartup },
            new TaskTriggerInfo { Type = TaskTriggerInfo.TriggerDaily, TimeOfDayTicks = TimeSpan.FromHours(2).Ticks }
        };
}