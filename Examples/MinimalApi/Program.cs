var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

//Singleton to keep sample data in memory for Scoped and Transient we'd have a new instance every request.
builder.Services.AddSingleton<MoviesRepository>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

#region DefaultExample
var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");
#endregion

#region MoviesExample
app.MapGet("/movies", async (MoviesRepository repository) => await repository.GetAll());

app.MapGet("/movies/{id:Guid}", async (Guid id, MoviesRepository repository) =>
await repository.GetById(id) is Movie movie
    ? Results.Ok(movie)
    : Results.NotFound()
);

app.MapPost("/movies", async (Movie movie, MoviesRepository repository) =>
{
    var newMovie = movie with { Id = Guid.NewGuid() }; // with creates a copy of an instance


    return await repository.Add(newMovie) is Movie createdMovie
    ? Results.Created($"/movies/{createdMovie.Id}", createdMovie)
    : Results.InternalServerError();
});

app.MapPut("/movies/{id:Guid}", async (Guid id, Movie movie, MoviesRepository repository) =>
{
    var existingMovie = await repository.GetById(id);
    if (existingMovie is null) return Results.NotFound();

    var updatedMovie = existingMovie with
    {
        Director = movie.Director,
        ReleaseYear = movie.ReleaseYear,
        Title = movie.Title
    };


    return Results.Ok(updatedMovie);

});
#endregion

app.Run();

#region Models
internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

internal record Movie(Guid Id, string Title, int ReleaseYear, Director Director); //For simplicity 1:1 relation
internal record Director(string Name, string Country);

internal class MoviesRepository
{
    private readonly List<Movie> _movies =
    [
        new Movie(Guid.NewGuid(), "Inception", 2010, new Director("Christopher Nolan", "UK")),
        new Movie(Guid.NewGuid(), "The Matrix", 1999, new Director("The Wachowskis", "USA")),
        new Movie(Guid.NewGuid(), "Interstellar", 2014, new Director("Christopher Nolan", "UK")),
    ];

    //Simulate async database calls
    public async Task<List<Movie>> GetAll() => await Task.FromResult(_movies);
    
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
}
#endregion
