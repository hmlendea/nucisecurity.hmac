using ArchUnitNET.Domain;
using ArchUnitNET.Loader;

using NuciSecurity.HMAC;

using static ArchUnitNET.Fluent.ArchRuleDefinition;

using Assembly = System.Reflection.Assembly;

namespace NuciSecurity.HMAC.ArchitectureTests
{
    internal static class ProductionArchitecture
    {
        internal static Assembly Assembly { get; } = typeof(HmacEncoder).Assembly;

        internal static Architecture Model { get; } = new ArchLoader()
            .LoadAssemblies(Assembly)
            .Build();

        internal static IObjectProvider<IType> ProductionTypes { get; } = Types()
            .That()
            .ResideInAssembly(Assembly)
            .As("NuciSecurity.HMAC production types");
    }
}
