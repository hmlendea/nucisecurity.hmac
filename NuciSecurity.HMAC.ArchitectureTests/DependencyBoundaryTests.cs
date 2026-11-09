using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.NUnit;

using NUnit.Framework;

using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace NuciSecurity.HMAC.ArchitectureTests
{
    [TestFixture]
    public sealed class DependencyBoundaryTests
    {
        [Test]
        public void GivenProductionAssembly_WhenLoaded_ThenOnlyTheProductionAssemblyIsLoaded()
            => Assert.That(
                ProductionArchitecture.Model.Assemblies.Select(assembly => assembly.Name),
                Is.EquivalentTo(new[] { ProductionArchitecture.Assembly.GetName().Name }));

        [Test]
        public void GivenProductionAssembly_WhenLoaded_ThenItContainsTheExpectedPublicTypes()
        {
            string[] typeNames = ProductionArchitecture.Assembly.GetTypes()
                .Where(type => type.IsPublic)
                .Select(GetTypeName)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();
            Assert.That(typeNames, Is.Not.Empty, "The production assembly must expose public types.");
            Assert.That(typeNames, Is.EquivalentTo(new[]
            {
                "NuciSecurity.HMAC.HmacEncoder",
                "NuciSecurity.HMAC.HmacIgnoreAttribute",
                "NuciSecurity.HMAC.HmacOrderAttribute",
                "NuciSecurity.HMAC.HmacValidator"
            }));
        }

        [TestCase("Microsoft.AspNetCore")]
        [TestCase("Microsoft.EntityFrameworkCore")]
        [TestCase("Amazon.DynamoDBv2")]
        [TestCase("Dapper")]
        [TestCase("MediatR")]
        [TestCase("Microsoft.Extensions.DependencyInjection")]
        [TestCase("Microsoft.Extensions.Logging")]
        [TestCase("NUnit")]
        [TestCase("Moq")]
        [TestCase("ArchUnit")]
        public void GivenProductionAssembly_WhenCheckingExternalDependencies_ThenForbiddenAssembliesAreNotReferenced(
            string forbiddenAssembly)
        {
            AssemblyName[] references = ProductionArchitecture.Assembly.GetReferencedAssemblies();

            Assert.That(references, Is.Not.Empty, "The production assembly references must be inspected.");
            Assert.That(
                references.Select(GetReferenceName).Where(reference =>
                    reference.Contains(forbiddenAssembly, StringComparison.OrdinalIgnoreCase)),
                Is.Empty,
                $"The production assembly must not reference {forbiddenAssembly}.");
        }

        [Test]
        public void GivenProductionAssembly_WhenCheckingExternalDependencies_ThenOnlyTheAllowedAssembliesAreReferenced()
        {
            string[] allowedAssemblies =
            [
                "System.Runtime",
                "System.Collections",
                "System.Linq",
                "System.Security.Cryptography",
                "NuciExtensions"
            ];

            AssemblyName[] references = ProductionArchitecture.Assembly.GetReferencedAssemblies();

            Assert.That(references, Is.Not.Empty, "The production assembly references must be inspected.");
            Assert.That(
                references.Select(GetReferenceName).Where(reference =>
                    !allowedAssemblies.Contains(reference, StringComparer.Ordinal)),
                Is.Empty,
                "The production assembly must only reference the documented assemblies.");
        }

        [Test]
        public void GivenValidatorTypes_WhenCheckingDependencies_ThenEncoderIsTheOnlyInternalDependency()
        {
            AssertHasProductionTypes();

            IObjectProvider<IType> validatorTypes = Types()
                .That()
                .HaveFullName("NuciSecurity.HMAC.HmacValidator");

            AssertHasTypes(validatorTypes, "the validator boundary");

            Types().That().Are(validatorTypes).Should().DependOnAny(
                Types().That().HaveFullName("NuciSecurity.HMAC.HmacEncoder"))
                .Because("the validator delegates token generation to the encoder")
                .Check(ProductionArchitecture.Model);
        }

        [TestCase("System.Net")]
        [TestCase("System.IO")]
        [TestCase("System.Threading")]
        [TestCase("System.Diagnostics")]
        [TestCase("Microsoft.AspNetCore")]
        [TestCase("Microsoft.Extensions")]
        [TestCase("Microsoft.EntityFrameworkCore")]
        public void GivenProductionSources_WhenCheckingExternalDependencies_ThenForbiddenNamespacesAreNotUsed(
            string forbiddenNamespace)
        {
            IReadOnlyList<string> sources = ArchitectureResources.GetResourcePaths(
                ArchitectureResources.ProductionSourcePrefix);

            Assert.That(sources, Is.Not.Empty, "No production source files were found.");

            foreach (string source in sources)
            {
                string content = ArchitectureResources.ReadResource(
                    ArchitectureResources.ProductionSourcePrefix,
                    source);

                Assert.That(
                    content,
                    Does.Not.Match($@"(?m)^\s*using\s+{Regex.Escape(forbiddenNamespace)}(\.[A-Za-z0-9_.]+)?\s*;"),
                    $"{source}: the {forbiddenNamespace} namespace must not be imported.");
            }
        }

        [Test]
        public void GivenEncoderTypes_WhenCheckingDependencies_ThenOnlyTheValidatorDelegationIsUsed()
        {
            AssertHasProductionTypes();

            IObjectProvider<IType> encoderTypes = Types()
                .That()
                .HaveFullName("NuciSecurity.HMAC.HmacEncoder");

            AssertHasTypes(encoderTypes, "the encoder boundary");

            IObjectProvider<MethodMember> generationMethods = MethodMembers()
                .That()
                .AreDeclaredIn(encoderTypes)
                .And()
                .HaveNameStartingWith("GenerateToken");

            AssertHasMembers(generationMethods, "the token generation boundary");

            // The encoder only reaches the validator through the obsolete
            // HmacEncoder.IsTokenValid delegation; the token generation path must not.
            MethodMembers().That().AreDeclaredIn(encoderTypes)
                .And().HaveNameStartingWith("GenerateToken")
                .Should().NotHaveDependencyInMethodBodyTo(
                    Types().That().HaveFullName("NuciSecurity.HMAC.HmacValidator"))
                .Because("the token generation path must not depend on the validator")
                .Check(ProductionArchitecture.Model);
        }

        [Test]
        public void GivenAttributeTypes_WhenCheckingDependencies_ThenTheyRemainMetadataOnly()
        {
            AssertHasProductionTypes();

            IObjectProvider<IType> attributeTypes = Types()
                .That()
                .HaveFullName("NuciSecurity.HMAC.HmacIgnoreAttribute")
                .Or()
                .HaveFullName("NuciSecurity.HMAC.HmacOrderAttribute");

            AssertHasTypes(attributeTypes, "the attribute boundary");

            Types().That().Are(attributeTypes).Should().NotDependOnAny(
                Types().That().HaveFullName("NuciSecurity.HMAC.HmacEncoder"))
                .Because("attributes are metadata only and must not depend on the encoder")
                .Check(ProductionArchitecture.Model);
        }

        [Test]
        public void GivenProductionTypes_WhenCheckingPublicSurface_ThenOnlyTheDocumentedNamespaceIsUsed()
        {
            AssertHasProductionTypes();

            IObjectProvider<IType> publicTypes = Types()
                .That()
                .Are(ProductionArchitecture.ProductionTypes)
                .And()
                .ArePublic();

            AssertHasTypes(publicTypes, "the public API surface");

            Types().That().Are(publicTypes).Should().ResideInNamespace("NuciSecurity.HMAC")
                .Because("the public API surface must stay inside the documented namespace")
                .Check(ProductionArchitecture.Model);
        }

        [Test]
        public void GivenProductionTypes_WhenCheckingState_ThenNoInstanceOrMutableStaticStateIsDeclared()
        {
            Type[] productionTypes = ProductionArchitecture.Assembly.GetTypes()
                .Where(type => type.Namespace is not null &&
                    type.Namespace.StartsWith("NuciSecurity.HMAC", StringComparison.Ordinal))
                .ToArray();

            Assert.That(productionTypes, Is.Not.Empty, "The production assembly must contain production types.");

            FieldInfo[] mutableInstanceFields = productionTypes
                .SelectMany(type => type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                .Where(field => !field.IsInitOnly)
                .ToArray();

            Assert.That(
                mutableInstanceFields,
                Is.Empty,
                "The library must stay stateless and must not declare mutable instance fields.");

            FieldInfo[] mutableStaticFields = productionTypes
                .SelectMany(type => type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                .Where(field => !field.IsInitOnly)
                .Where(field => !IsCompilerGenerated(field))
                .ToArray();

            Assert.That(
                mutableStaticFields,
                Is.Empty,
                "The library must not declare mutable static fields.");
        }

        [Test]
        public void GivenProductionAssembly_WhenCheckingReferences_ThenTestDependenciesAreAbsent()
        {
            AssemblyName[] references = ProductionArchitecture.Assembly.GetReferencedAssemblies();

            Assert.That(references, Is.Not.Empty, "The production assembly references must be inspected.");
            Assert.That(
                references.Select(GetReferenceName).Where(IsTestDependency),
                Is.Empty,
                "The production assembly must remain independent of test assemblies and frameworks.");
        }

        [Test]
        public void GivenProductionAssembly_WhenCheckingFriendAssemblies_ThenInternalsAreNotExposed()
            => Assert.That(
                ProductionArchitecture.Assembly.GetCustomAttributesData()
                    .Where(attribute => attribute.AttributeType == typeof(InternalsVisibleToAttribute)),
                Is.Empty,
                "The production assembly must not expose internals to friend assemblies.");

        private static void AssertHasProductionTypes()
            => AssertHasTypes(ProductionArchitecture.ProductionTypes, "the production boundary");

        private static string GetTypeName(Type type)
        {
            if (type.FullName is null)
            {
                return type.Name;
            }

            return type.FullName;
        }

        private static string GetReferenceName(AssemblyName reference)
        {
            if (reference.Name is null)
            {
                return string.Empty;
            }

            return reference.Name;
        }

        private static bool IsCompilerGenerated(FieldInfo field)
            => field.IsDefined(typeof(CompilerGeneratedAttribute), false) ||
                field.DeclaringType is not null &&
                field.DeclaringType.IsDefined(typeof(CompilerGeneratedAttribute), false);

        private static void AssertHasTypes(IObjectProvider<IType> selection, string boundaryName)
            => Assert.That(
                selection.GetObjects(ProductionArchitecture.Model),
                Is.Not.Empty,
                $"The {boundaryName} must select production types.");

        private static void AssertHasMembers(IObjectProvider<MethodMember> selection, string boundaryName)
            => Assert.That(
                selection.GetObjects(ProductionArchitecture.Model),
                Is.Not.Empty,
                $"The {boundaryName} must select production members.");

        private static bool IsTestDependency(string? reference)
            => reference is not null &&
                (reference.Contains("Tests", StringComparison.OrdinalIgnoreCase) ||
                 reference.Contains("NUnit", StringComparison.OrdinalIgnoreCase) ||
                 reference.Contains("Moq", StringComparison.OrdinalIgnoreCase) ||
                 reference.Contains("ArchUnit", StringComparison.OrdinalIgnoreCase) ||
                 reference.Contains("Test.Sdk", StringComparison.OrdinalIgnoreCase) ||
                 reference.Contains("testhost", StringComparison.OrdinalIgnoreCase) ||
                 reference.Contains("coverlet", StringComparison.OrdinalIgnoreCase));
    }
}
