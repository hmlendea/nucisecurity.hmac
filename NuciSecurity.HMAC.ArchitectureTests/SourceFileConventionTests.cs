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
    public sealed class SourceFileConventionTests
    {
        [TestCase(ArchitectureResources.ProductionSourcePrefix)]
        [TestCase(ArchitectureResources.UnitTestSourcePrefix)]
        [TestCase(ArchitectureResources.ArchitectureTestSourcePrefix)]
        public void GivenSources_WhenChecked_ThenEveryFileDeclaresOneMatchingType(string sourcePrefix)
        {
            IReadOnlyList<string> sources = ArchitectureResources.GetResourcePaths(sourcePrefix);

            Assert.That(sources, Is.Not.Empty, $"No source files were found under '{sourcePrefix}'.");

            foreach (string source in sources)
            {
                IReadOnlyList<string> declaredTypes = GetDeclaredTypeNames(sourcePrefix, source);

                Assert.That(
                    declaredTypes,
                    Has.Count.EqualTo(1),
                    $"{source}: expected exactly one declared type, but found {declaredTypes.Count}.");

                Assert.That(
                    declaredTypes[0],
                    Is.EqualTo(GetFileStem(source)),
                    $"{source}: the declared type must match the file name.");
            }
        }

        [TestCase(ArchitectureResources.ProductionSourcePrefix)]
        [TestCase(ArchitectureResources.UnitTestSourcePrefix)]
        [TestCase(ArchitectureResources.ArchitectureTestSourcePrefix)]
        public void GivenSources_WhenChecked_ThenTheyUseBlockNamespaces(string sourcePrefix)
        {
            IReadOnlyList<string> sources = ArchitectureResources.GetResourcePaths(sourcePrefix);

            Assert.That(sources, Is.Not.Empty, $"No source files were found under '{sourcePrefix}'.");

            foreach (string source in sources)
            {
                string content = ArchitectureResources.ReadResource(sourcePrefix, source);

                Assert.That(
                    content,
                    Does.Not.Match(@"(?m)^\s*namespace\s+[A-Za-z_][A-Za-z0-9_.]*\s*;"),
                    $"{source}: file-scoped namespaces are not allowed.");
            }
        }

        [TestCase(ArchitectureResources.ProductionSourcePrefix)]
        [TestCase(ArchitectureResources.UnitTestSourcePrefix)]
        [TestCase(ArchitectureResources.ArchitectureTestSourcePrefix)]
        public void GivenSources_WhenChecked_ThenTheyDoNotUseNullableDirectives(string sourcePrefix)
        {
            IReadOnlyList<string> sources = ArchitectureResources.GetResourcePaths(sourcePrefix);

            Assert.That(sources, Is.Not.Empty, $"No source files were found under '{sourcePrefix}'.");

            foreach (string source in sources)
            {
                string content = ArchitectureResources.ReadResource(sourcePrefix, source);

                Assert.That(
                    content,
                    Does.Not.Match(@"(?m)^\s*#nullable\s+"),
                    $"{source}: the nullable directive is not allowed.");
            }
        }

        [TestCase(ArchitectureResources.ProductionSourcePrefix)]
        [TestCase(ArchitectureResources.UnitTestSourcePrefix)]
        [TestCase(ArchitectureResources.ArchitectureTestSourcePrefix)]
        public void GivenSources_WhenChecked_ThenTheyDoNotUseRegions(string sourcePrefix)
        {
            IReadOnlyList<string> sources = ArchitectureResources.GetResourcePaths(sourcePrefix);

            Assert.That(sources, Is.Not.Empty, $"No source files were found under '{sourcePrefix}'.");

            foreach (string source in sources)
            {
                string content = ArchitectureResources.ReadResource(sourcePrefix, source);

                Assert.That(
                    content,
                    Does.Not.Match(@"(?m)^\s*#(end)?region\b"),
                    $"{source}: regions are not allowed.");
            }
        }

        private static IReadOnlyList<string> GetDeclaredTypeNames(string sourcePrefix, string source)
        {
            SyntaxTree tree = CSharpSyntaxTree.ParseText(
                ArchitectureResources.ReadResource(sourcePrefix, source),
                path: source);

            IEnumerable<string> typeDeclarations = tree.GetRoot()
                .DescendantNodes()
                .OfType<BaseTypeDeclarationSyntax>()
                .Select(node => node.Identifier.ValueText);

            IEnumerable<string> delegateDeclarations = tree.GetRoot()
                .DescendantNodes()
                .OfType<DelegateDeclarationSyntax>()
                .Select(node => node.Identifier.ValueText);

            return typeDeclarations.Concat(delegateDeclarations).ToArray();
        }

        private static string GetFileStem(string source)
        {
            string fileName = source.Substring(source.LastIndexOf('/') + 1);

            return fileName.Substring(0, fileName.LastIndexOf('.'));
        }
    }
}
