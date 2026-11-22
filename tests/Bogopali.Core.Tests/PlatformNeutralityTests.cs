using System.Reflection;
using System.Runtime.Versioning;

namespace Bogopali.Core.Tests;

public sealed class PlatformNeutralityTests
{
    private static readonly Assembly s_core = Assembly.Load("Bogopali.Core");

    // Pure managed libraries the Core may use. Each must be platform-neutral; the Linux CI job
    // running these tests is the real guard.
    private static readonly string[] s_allowedLibraries = ["Tomlyn"];

    [Fact]
    public void CoreReferencesOnlyTheBaseClassLibraryAndAllowedLibraries()
    {
        string[] nonFramework = s_core.GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .Where(name => name != "netstandard" && !name.StartsWith("System", StringComparison.Ordinal))
            .Except(s_allowedLibraries, StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(nonFramework);
    }

    [Fact]
    public void CoreDoesNotTargetASpecificOperatingSystem()
    {
        Assert.Null(s_core.GetCustomAttribute<SupportedOSPlatformAttribute>());
        Assert.Null(s_core.GetCustomAttribute<TargetPlatformAttribute>());
    }
}
