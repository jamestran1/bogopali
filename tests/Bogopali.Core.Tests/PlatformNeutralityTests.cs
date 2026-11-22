using System.Reflection;
using System.Runtime.Versioning;

namespace Bogopali.Core.Tests;

public sealed class PlatformNeutralityTests
{
    private static readonly Assembly s_core = Assembly.Load("Bogopali.Core");

    [Fact]
    public void CoreReferencesOnlyTheBaseClassLibrary()
    {
        string[] nonFramework = s_core.GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .Where(name => name != "netstandard" && !name.StartsWith("System", StringComparison.Ordinal))
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
