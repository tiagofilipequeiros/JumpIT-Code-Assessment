using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi;
using Products.Api.Auth;
using Products.Api.Health;
using Products.Api.Errors;
using Products.Application;
using Products.Application.Abstractions;
using Products.Infrastructure;
using Products.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Errors: every error response is ProblemDetails with a "code" field.
builder.Services.AddProblemDetails(options =>
    options.CustomizeProblemDetails = context =>
        context.ProblemDetails.Extensions.TryAdd("code",
            ErrorCodeMappings.FromStatus(context.ProblemDetails.Status ?? StatusCodes.Status500InternalServerError).ToString()));
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Layers.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration.GetConnectionString("Default"));
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserAccessor, HeaderCurrentUserAccessor>();

// API. Enums are sent as text ("Admin", "ProductNotFound", ...); validation errors use the JSON field names ("name").
builder.Services.AddControllers(options => options.ModelMetadataDetailsProviders.Add(new SystemTextJsonValidationMetadataProvider()))
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Health: 200 when the database answers, 503 when it doesn't.
builder.Services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database");

// Swagger docs at /docs, with an "Authorize" box to set the X-User-Id header.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "Products.Api.xml"));
    options.AddSecurityDefinition("UserId", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Name = HeaderCurrentUserAccessor.HeaderName,
        Description = "User ID: 1 = Admin, 2 = Editor, 3 and 4 = User.",
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("UserId", document)] = [],
    });
});

var app = builder.Build();

await app.Services.MigrateDatabaseAsync();
if (app.Configuration.GetValue<bool>("Seeding:DemoActivity"))
{
    await app.Services.SeedDemoActivityAsync();
}

app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.RoutePrefix = "docs";
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
});

app.MapControllers();

app.MapHealthChecks("/api/health", new HealthCheckOptions
{
    ResponseWriter = (context, report) =>
    {
        static string Text(HealthStatus status) => status == HealthStatus.Healthy ? "ok" : "unavailable";
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            status = Text(report.Status),
            database = report.Entries.TryGetValue("database", out var database) ? Text(database.Status) : "unknown",
        }));
    },
});

await app.RunAsync();

// Lets the integration tests start the API.
public partial class Program;
