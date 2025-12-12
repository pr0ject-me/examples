namespace MinimalApi.Movies;

public static class MoviesEndpoints
{
    public static void MapMoviesEndpoints(this WebApplication app)
    {
        app.MapGet("/movies", async (MoviesInMemoryRepository repository) => await repository.GetAll());

        app.MapGet("/movies/{id:Guid}", async (Guid id, MoviesInMemoryRepository repository) =>
        await repository.GetById(id) is Movie movie
            ? Results.Ok(movie)
            : Results.NotFound()
        );

        app.MapPost("/movies", async (Movie movie, MoviesInMemoryRepository repository) =>
        {
            var newMovie = movie with { Id = Guid.NewGuid() }; // with creates a copy of an instance

            return await repository.Add(newMovie) is Movie createdMovie
            ? Results.Created($"/movies/{createdMovie.Id}", createdMovie)
            : Results.InternalServerError();
        });

        app.MapPut("/movies/{id:Guid}", async (Guid id, Movie movie, MoviesInMemoryRepository repository) =>
        {
            var existingMovie = await repository.GetById(id);
            if (existingMovie is null) return Results.NotFound();

            var updatedMovie = existingMovie with
            {
                DirectorName = movie.DirectorName,
                ReleaseYear = movie.ReleaseYear,
                Title = movie.Title
            };

            return Results.Ok(updatedMovie);

        });

        app.MapDelete("/movies/{id:Guid}", async (Guid id, MoviesInMemoryRepository repository) =>
        {
            var movie = await repository.GetById(id);
            if (movie is null) return Results.NotFound();

            await repository.Delete(id);
            return Results.NoContent();
        });
    }
}


public record Movie(Guid Id, string Title, int ReleaseYear, string DirectorName); //For simplicity 1:1 relation

public class MoviesInMemoryRepository
{
    private readonly List<Movie> _movies =
    [
        new Movie(Guid.NewGuid(), "Inception", 2010, "Nolan"),
        new Movie(Guid.NewGuid(), "The Matrix", 1999, "Wachowski"),
        new Movie(Guid.NewGuid(), "Interstellar", 2014,"Nolan"),
    ];

    //Simulate async database calls
    //Async Await is not needed when we pass Task to a caller where it can be awaited.
    public Task<List<Movie>> GetAll() => Task.FromResult(_movies);

    public async Task<Movie?> GetById(Guid id) => await Task.FromResult(_movies.FirstOrDefault(m => m.Id == id));

    public async Task<Movie?> Add(Movie movie)
    {
        _movies.Add(movie);
        return await Task.FromResult(movie);
    }

    public async Task<Movie?> Update(Movie movie)
    {
        var index = _movies.FindIndex(x => x.Id == movie.Id);
        if (index == -1) return default;

        _movies[index] = movie;
        return movie;
    }

    public async Task<bool> Delete(Guid id)
    {
        var movie = _movies.FindIndex(m => m.Id == id);
        if (movie == -1) return await Task.FromResult(false);

        _movies.RemoveAt(movie);
        return await Task.FromResult(true);
    }
}
