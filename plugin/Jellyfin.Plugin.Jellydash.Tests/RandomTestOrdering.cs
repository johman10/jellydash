using Xunit.Abstractions;
using Xunit.Sdk;

[assembly: TestCaseOrderer(
    "Jellyfin.Plugin.Jellydash.Tests.RandomTestCaseOrderer",
    "Jellyfin.Plugin.Jellydash.Tests")]

namespace Jellyfin.Plugin.Jellydash.Tests;

/// <summary>
/// xUnit test case orderer that runs test methods in a random order on
/// each test run. This helps to surface hidden ordering dependencies
/// between tests.
///
/// To reproduce a failing random order deterministically, set the
/// JELLYDASH_TEST_SEED environment variable to a fixed integer value.
/// </summary>
public sealed class RandomTestCaseOrderer : ITestCaseOrderer
{
    private readonly Random _random;

    public RandomTestCaseOrderer(IMessageSink diagnosticMessageSink)
    {
        _random = CreateRandom();
    }

    private static Random CreateRandom()
    {
        var seedVar = Environment.GetEnvironmentVariable("JELLYDASH_TEST_SEED");
        if (int.TryParse(seedVar, out var seed))
        {
            return new Random(seed);
        }

        return new Random();
    }

    public IEnumerable<TTestCase> OrderTestCases<TTestCase>(IEnumerable<TTestCase> testCases)
        where TTestCase : ITestCase
    {
        // OrderBy with a random key is sufficient for small test sets.
        return testCases.OrderBy(_ => _random.Next());
    }
}
