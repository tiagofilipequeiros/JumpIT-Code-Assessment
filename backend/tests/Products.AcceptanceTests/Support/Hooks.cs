using Products.TestSupport;
using Reqnroll;

namespace Products.AcceptanceTests.Support;

[Binding]
public sealed class Hooks
{
    // One SQL Server container for the whole run; every scenario gets its own database.
    private static readonly SqlServerContainer SqlServer = new();

    [BeforeTestRun]
    public static Task StartSqlServer() => SqlServer.StartAsync();

    [AfterTestRun]
    public static async Task StopSqlServer() => await SqlServer.DisposeAsync();

    [BeforeScenario]
    public static void StartApi(ScenarioApi api) => api.Start(SqlServer.NewDatabase());
}
