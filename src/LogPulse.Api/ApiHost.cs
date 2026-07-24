using LogPulse.Domain;
using LogPulse.Parsing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using LogLevel = LogPulse.Domain.LogLevel;

namespace LogPulse.Api;

public static class ApiHost
{
    public static WebApplication Build(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        builder.Services.AddSingleton<InMemoryEventStore>();
        builder.Services.AddSingleton<PlainTextParser>();

        WebApplication app = builder.Build();
        RouteGroupBuilder api = app.MapGroup("/api");

        api.MapGet("/health", () => Results.Text("healthy"));

        api.MapPost("/logs",
            async (HttpRequest request, [FromServices] PlainTextParser parser, InMemoryEventStore store) =>
            {
                using StreamReader reader = new StreamReader(request.Body);
                string body = await reader.ReadToEndAsync(request.HttpContext.RequestAborted);

                ParseContext context = request.Query.TryGetValue("service", out StringValues service) && service.Count > 0
                    ? ParseContext.Now(service[0]!)
                    : ParseContext.Now();

                int accepted = 0;
                int rejected = 0;
                foreach (string rawLine in body.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (parser.TryParse(rawLine.AsSpan().Trim(), in context, out LogEvent? logEvent))
                    {
                        store.Add(logEvent);
                        accepted++;
                    }
                    else
                    {
                        rejected++;
                    }
                }

                return Results.Accepted(value: new { accepted, rejected });
            });

        api.MapGet("/logs/recent-errors",
            (InMemoryEventStore store, int minutes = 15, int take = 3, string? service = null) => 
            {
                TimeRange range = TimeRange.Past(TimeSpan.FromMinutes(minutes));
                IEnumerable<LogEvent> query = store.Query(range)
                                                   .Where(e => e.Level.IsAtLeast(LogLevel.Error));
                if (service is not null)
                    query = query.Where(e => string.Equals(e.Service, service, StringComparison.OrdinalIgnoreCase));

                return Results.Ok(query.Take(take).ToList());
            });

        return app;
    }
}
