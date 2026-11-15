using Microsoft.AspNetCore.Http.HttpResults;
using Shortener.Api;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();
builder.Services.Configure<ShortenerOptions>(builder.Configuration.GetSection("Shortener"));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IPersistenceStore, JsonFilePersistenceStore>();
builder.Services.AddSingleton<ShortenerService>();

var app = builder.Build();
app.UseExceptionHandler();
app.MapHealthChecks("/health");

app.MapPost("/api/v1/links", async Task<Results<Created<LinkResponse>, ProblemHttpResult>> (CreateLinkRequest request, ShortenerService service, CancellationToken cancellationToken) =>
{
    try { var response = await service.CreateAsync(request, cancellationToken); return TypedResults.Created($"/api/v1/links/{response.Code}", response); }
    catch (ValidationException ex) { return TypedResults.Problem(ex.Message, statusCode: 400); }
    catch (ConflictException ex) { return TypedResults.Problem(ex.Message, statusCode: 409); }
});

app.MapGet("/{code}", async Task<Results<RedirectHttpResult, ProblemHttpResult>> (string code, HttpContext context, ShortenerService service, CancellationToken cancellationToken) =>
{
    try
    {
        var result = await service.ResolveAsync(code, context.Connection.RemoteIpAddress?.ToString(), context.Request.Headers.UserAgent, cancellationToken);
        return TypedResults.Redirect(result.Url);
    }
    catch (LinkNotFoundException ex) { return TypedResults.Problem(ex.Message, statusCode: 404); }
    catch (LinkExpiredException ex) { return TypedResults.Problem(ex.Message, statusCode: 410); }
});

app.MapGet("/api/v1/links/{code}/analytics", async Task<Results<Ok<AnalyticsResponse>, ProblemHttpResult>> (string code, ShortenerService service, CancellationToken cancellationToken) =>
{
    try { return TypedResults.Ok(await service.GetAnalyticsAsync(code, cancellationToken)); }
    catch (LinkNotFoundException ex) { return TypedResults.Problem(ex.Message, statusCode: 404); }
});

app.Run();

public partial class Program { }
