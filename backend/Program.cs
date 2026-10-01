using System.Text.Json.Serialization;
using Backend.Auth;
using Backend.Data;
using Backend.Errors;
using Backend.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Allow the Angular app to call this API.
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// Errors: every error response is ProblemDetails with a "code" field.
builder.Services.AddProblemDetails(options =>
    options.CustomizeProblemDetails = context =>
        context.ProblemDetails.Extensions.TryAdd("code",
            ErrorCodes.FromStatus(context.ProblemDetails.Status ?? StatusCodes.Status500InternalServerError).ToString()));
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Database.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// API. Enums are sent as text ("Admin", "ProductNotFound", ...).
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<CurrentUser>();
builder.Services.AddScoped<MetricsService>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<CategoryService>();
builder.Services.AddScoped<UserService>();

// Swagger docs at /docs, with an "Authorize" box to set the X-User-Id header.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "Backend.xml"));
    options.AddSecurityDefinition("UserId", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Name = CurrentUser.HeaderName,
        Description = "User ID: 1 = Admin, 2 = Editor, 3 and 4 = User.",
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("UserId", document)] = [],
    });
});

var app = builder.Build();

// Create or update the database to the latest migration on startup.
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
}

app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseCors();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.RoutePrefix = "docs";
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
});

app.MapControllers();

app.MapGet("/api/health", async (AppDbContext db) => new
{
    status = "ok",
    database = await db.Database.CanConnectAsync() ? "ok" : "unavailable"
});

app.Run();

// Lets the integration tests start the API.
public partial class Program;
