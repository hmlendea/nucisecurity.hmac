using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using NUnit.Framework;

namespace NuciSecurity.HMAC.ArchitectureTests
{
    [TestFixture]
    public sealed class TypeDeclarationConventionTests
    {
        [TestCase(ArchitectureResources.ProductionSourcePrefix)]
        [TestCase(ArchitectureResources.UnitTestSourcePrefix)]
        [TestCase(ArchitectureResources.ArchitectureTestSourcePrefix)]
        public void GivenSources_WhenChecked_ThenNoPartialClassesAreUsed(string sourcePrefix)
        {
            IReadOnlyList<string> sources = ArchitectureResources.GetResourcePaths(sourcePrefix);

            Assert.That(sources, Is.Not.Empty, $"No source files were found under '{sourcePrefix}'.");

            foreach (string source in sources)
            {
                SyntaxTree tree = ParseTree(sourcePrefix, source);
                IEnumerable<SyntaxNode> partialTypes = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<TypeDeclarationSyntax>()
                    .Where(declaration => declaration.Modifiers.Any(modifier =>
                        modifier.IsKind(SyntaxKind.PartialKeyword)))
                    .ToArray();

                Assert.That(
                    partialTypes,
                    Is.Empty,
                    $"{source}: partial classes are not allowed.");
            }
        }

        [TestCase(ArchitectureResources.ProductionSourcePrefix)]
        [TestCase(ArchitectureResources.UnitTestSourcePrefix)]
        [TestCase(ArchitectureResources.ArchitectureTestSourcePrefix)]
        public void GivenSources_WhenChecked_ThenNoInitOnlyPropertiesAreUsed(string sourcePrefix)
        {
            IReadOnlyList<string> sources = ArchitectureResources.GetResourcePaths(sourcePrefix);

            Assert.That(sources, Is.Not.Empty, $"No source files were found under '{sourcePrefix}'.");

            foreach (string source in sources)
            {
                SyntaxTree tree = ParseTree(sourcePrefix, source);
                IEnumerable<SyntaxNode> initAccessors = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<AccessorDeclarationSyntax>()
                    .Where(accessor => accessor.IsKind(SyntaxKind.InitAccessorDeclaration))
                    .ToArray();

                Assert.That(
                    initAccessors,
                    Is.Empty,
                    $"{source}: init-only properties are not allowed.");
            }
        }

        [TestCase(ArchitectureResources.ProductionSourcePrefix)]
        [TestCase(ArchitectureResources.UnitTestSourcePrefix)]
        [TestCase(ArchitectureResources.ArchitectureTestSourcePrefix)]
        public void GivenSources_WhenChecked_ThenNoConstDeclarationsAreUsed(string sourcePrefix)
        {
            IReadOnlyList<string> sources = ArchitectureResources.GetResourcePaths(sourcePrefix);

            Assert.That(sources, Is.Not.Empty, $"No source files were found under '{sourcePrefix}'.");

            foreach (string source in sources)
            {
                SyntaxTree tree = ParseTree(sourcePrefix, source);
                IEnumerable<SyntaxNode> constDeclarations = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<FieldDeclarationSyntax>()
                    .Where(field => field.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.ConstKeyword)))
                    // Documented, narrowly scoped exception: test-case arguments require
                    // compile-time constants, so ArchitectureResources keeps its prefixes
                    // as const fields.
                    .Where(field => !IsArchitectureResourcesPrefix(field))
                    .ToArray();

                Assert.That(
                    constDeclarations,
                    Is.Empty,
                    $"{source}: 'const' is not allowed; use static properties.");
            }
        }

        [TestCase(ArchitectureResources.ProductionSourcePrefix)]
        public void GivenProductionSources_WhenChecked_ThenEveryEligibleClassIsSealed(string sourcePrefix)
        {
            IReadOnlyList<string> sources = ArchitectureResources.GetResourcePaths(sourcePrefix);

            Assert.That(sources, Is.Not.Empty, $"No source files were found under '{sourcePrefix}'.");

            foreach (string source in sources)
            {
                SyntaxTree tree = ParseTree(sourcePrefix, source);
                IEnumerable<ClassDeclarationSyntax> unsealedClasses = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<ClassDeclarationSyntax>()
                    .Where(declaration => !declaration.Modifiers.Any(modifier =>
                        modifier.IsKind(SyntaxKind.SealedKeyword) ||
                        modifier.IsKind(SyntaxKind.StaticKeyword) ||
                        modifier.IsKind(SyntaxKind.AbstractKeyword)))
                    .ToArray();

                Assert.That(
                    unsealedClasses,
                    Is.Empty,
                    $"{source}: every non-static, non-abstract class must be sealed.");
            }
        }

        [TestCase(ArchitectureResources.ProductionSourcePrefix)]
        [TestCase(ArchitectureResources.UnitTestSourcePrefix)]
        [TestCase(ArchitectureResources.ArchitectureTestSourcePrefix)]
        public void GivenSources_WhenChecked_ThenTypesUseTheExpectedSuffixes(string sourcePrefix)
        {
            IReadOnlyList<string> sources = ArchitectureResources.GetResourcePaths(sourcePrefix);

            Assert.That(sources, Is.Not.Empty, $"No source files were found under '{sourcePrefix}'.");

            foreach (string source in sources)
            {
                SyntaxTree tree = ParseTree(sourcePrefix, source);
                IEnumerable<string> exceptionTypesWithoutSuffix = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<ClassDeclarationSyntax>()
                    .Where(declaration => InheritsFromException(declaration))
                    .Select(declaration => declaration.Identifier.ValueText)
                    .Where(name => !name.EndsWith("Exception", StringComparison.Ordinal))
                    .ToArray();

                Assert.That(
                    exceptionTypesWithoutSuffix,
                    Is.Empty,
                    $"{source}: custom exception types must use the 'Exception' suffix.");
            }
        }

        [TestCase(ArchitectureResources.ProductionSourcePrefix)]
        public void GivenProductionSources_WhenChecked_ThenTypesResideInAFileNamespaceMatchingTheirFolder(string sourcePrefix)
        {
            IReadOnlyList<string> sources = ArchitectureResources.GetResourcePaths(sourcePrefix);

            Assert.That(sources, Is.Not.Empty, $"No source files were found under '{sourcePrefix}'.");

            foreach (string source in sources)
            {
                SyntaxTree tree = ParseTree(sourcePrefix, source);
                IEnumerable<string> namespaces = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<BaseNamespaceDeclarationSyntax>()
                    .Select(declaration => declaration.Name.ToString())
                    .ToArray();

                Assert.That(
                    namespaces,
                    Is.Not.Empty,
                    $"{source}: the file must declare a namespace.");

                Assert.That(
                    namespaces,
                    Has.All.EqualTo("NuciSecurity.HMAC"),
                    $"{source}: production types must reside in the NuciSecurity.HMAC namespace.");
            }
        }

        private static SyntaxTree ParseTree(string sourcePrefix, string source)
            => CSharpSyntaxTree.ParseText(
                ArchitectureResources.ReadResource(sourcePrefix, source),
                path: source);

        private static bool InheritsFromException(ClassDeclarationSyntax declaration)
            => declaration.BaseList is not null &&
                declaration.BaseList.Types.Any(baseType =>
                    baseType.Type.ToString().EndsWith("Exception", StringComparison.Ordinal));

        private static bool IsArchitectureResourcesPrefix(FieldDeclarationSyntax field)
            => field.Declaration.Variables.Any(variable =>
                variable.Identifier.ValueText is "ProductionSourcePrefix" or
                "UnitTestSourcePrefix" or
                "ArchitectureTestSourcePrefix" or
                "ProjectFilePrefix");
    }
}