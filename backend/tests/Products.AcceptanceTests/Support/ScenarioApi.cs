using Products.TestSupport;

namespace Products.AcceptanceTests.Support;

// Per-scenario state, created and disposed by Reqnroll: the API, who is signed in, and the last responses.
public sealed class ScenarioApi : IDisposable
{
    private static readonly Dictionary<string, int> UserIds = new()
    {
        ["Alex Admin"] = 1,
        ["Erin Editor"] = 2,
        ["Sam User"] = 3,
        ["Taylor User"] = 4,
    };

    private ApiFactory? _api;

    public int? SignedInUserId { get; set; }
    public List<HttpResponseMessage> Responses { get; } = [];
    public HttpResponseMessage LastResponse => Responses[^1];

    public void Start(string connectionString)
    {
        _api = new ApiFactory(connectionString);
        _api.CreateClient(); // Starts the API: migrations and seed data.
    }

    public static int UserId(string name) =>
        UserIds.TryGetValue(name, out var id) ? id : throw new ArgumentException($"Unknown user '{name}'.");

    public HttpClient Client(int? userId = null) => _api!.ClientFor(userId ?? SignedInUserId);

    public HttpClient ClientFor(string userName) => Client(UserId(userName));

    public void Dispose() => _api?.Dispose();
}
