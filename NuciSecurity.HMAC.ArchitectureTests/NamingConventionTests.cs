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
    public sealed class NamingConventionTests
    {
        [TestCase(ArchitectureResources.ProductionSourcePrefix)]
        [TestCase(ArchitectureResources.UnitTestSourcePrefix)]
        [TestCase(ArchitectureResources.ArchitectureTestSourcePrefix)]
        public void GivenSources_WhenChecked_ThenNoBclTypeNamesAreUsed(string sourcePrefix)
        {
            IReadOnlyList<string> sources = ArchitectureResources.GetResourcePaths(sourcePrefix);

            Assert.That(sources, Is.Not.Empty, $"No source files were found under '{sourcePrefix}'.");

            foreach (string source in sources)
            {
                SyntaxTree tree = ParseTree(sourcePrefix, source);
                IEnumerable<string> bclTypeUsages = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<IdentifierNameSyntax>()
                    .Select(identifier => identifier.Identifier.ValueText)
                    .Where(IsBclTypeName)
                    .Distinct(StringComparer.Ordinal)
                    .ToArray();

                Assert.That(
                    bclTypeUsages,
                    Is.Empty,
                    $"{source}: built-in aliases must be used instead of {{ {string.Join(", ", bclTypeUsages)} }}.");
            }
        }

        [TestCase(ArchitectureResources.ProductionSourcePrefix)]
        [TestCase(ArchitectureResources.UnitTestSourcePrefix)]
        [TestCase(ArchitectureResources.ArchitectureTestSourcePrefix)]
        public void GivenSources_WhenChecked_ThenClassesUsePascalCase(string sourcePrefix)
        {
            IReadOnlyList<string> sources = ArchitectureResources.GetResourcePaths(sourcePrefix);

            Assert.That(sources, Is.Not.Empty, $"No source files were found under '{sourcePrefix}'.");

            foreach (string source in sources)
            {
                SyntaxTree tree = ParseTree(sourcePrefix, source);
                IEnumerable<string> nonPascalClasses = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<ClassDeclarationSyntax>()
                    .Select(declaration => declaration.Identifier.ValueText)
                    .Where(name => !IsPascalCase(name))
                    .ToArray();

                Assert.That(
                    nonPascalClasses,
                    Is.Empty,
                    $"{source}: class names must use PascalCase.");
            }
        }

        [TestCase(ArchitectureResources.ProductionSourcePrefix)]
        [TestCase(ArchitectureResources.UnitTestSourcePrefix)]
        [TestCase(ArchitectureResources.ArchitectureTestSourcePrefix)]
        public void GivenSources_WhenChecked_ThenMethodsUsePascalCase(string sourcePrefix)
        {
            IReadOnlyList<string> sources = ArchitectureResources.GetResourcePaths(sourcePrefix);

            Assert.That(sources, Is.Not.Empty, $"No source files were found under '{sourcePrefix}'.");

            foreach (string source in sources)
            {
                SyntaxTree tree = ParseTree(sourcePrefix, source);
                IEnumerable<string> nonPascalMethods = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<MethodDeclarationSyntax>()
                    .Select(method => method.Identifier.ValueText)
                    .Where(name => !IsPascalCase(name))
                    .ToArray();

                Assert.That(
                    nonPascalMethods,
                    Is.Empty,
                    $"{source}: method names must use PascalCase.");
            }
        }

        [TestCase(ArchitectureResources.ProductionSourcePrefix)]
        [TestCase(ArchitectureResources.UnitTestSourcePrefix)]
        [TestCase(ArchitectureResources.ArchitectureTestSourcePrefix)]
        public void GivenSources_WhenChecked_ThenPrivateFieldsUseCamelCaseWithoutUnderscore(string sourcePrefix)
        {
            IReadOnlyList<string> sources = ArchitectureResources.GetResourcePaths(sourcePrefix);

            Assert.That(sources, Is.Not.Empty, $"No source files were found under '{sourcePrefix}'.");

            foreach (string source in sources)
            {
                SyntaxTree tree = ParseTree(sourcePrefix, source);
                IEnumerable<VariableDeclaratorSyntax> underscoreFields = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<FieldDeclarationSyntax>()
                    .Where(field => field.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.PrivateKeyword)))
                    .SelectMany(field => field.Declaration.Variables)
                    .Where(variable => variable.Identifier.ValueText.StartsWith('_'))
                    .ToArray();

                Assert.That(
                    underscoreFields,
                    Is.Empty,
                    $"{source}: private fields must use camelCase without an underscore prefix.");
            }
        }

        [TestCase(ArchitectureResources.ProductionSourcePrefix)]
        [TestCase(ArchitectureResources.UnitTestSourcePrefix)]
        [TestCase(ArchitectureResources.ArchitectureTestSourcePrefix)]
        public void GivenSources_WhenChecked_ThenInterfacesUseTheIPrefix(string sourcePrefix)
        {
            IReadOnlyList<string> sources = ArchitectureResources.GetResourcePaths(sourcePrefix);

            Assert.That(sources, Is.Not.Empty, $"No source files were found under '{sourcePrefix}'.");

            foreach (string source in sources)
            {
                SyntaxTree tree = ParseTree(sourcePrefix, source);
                IEnumerable<string> nonPrefixedInterfaces = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<InterfaceDeclarationSyntax>()
                    .Select(declaration => declaration.Identifier.ValueText)
                    .Where(name => !name.StartsWith('I') || name.Length < 2 || !char.IsUpper(name[1]))
                    .ToArray();

                Assert.That(
                    nonPrefixedInterfaces,
                    Is.Empty,
                    $"{source}: interface names must use the 'I' prefix followed by PascalCase.");
            }
        }

        [TestCase(ArchitectureResources.ProductionSourcePrefix)]
        [TestCase(ArchitectureResources.UnitTestSourcePrefix)]
        [TestCase(ArchitectureResources.ArchitectureTestSourcePrefix)]
        public void GivenSources_WhenChecked_ThenBooleansUsePermittedForms(string sourcePrefix)
        {
            IReadOnlyList<string> sources = ArchitectureResources.GetResourcePaths(sourcePrefix);

            Assert.That(sources, Is.Not.Empty, $"No source files were found under '{sourcePrefix}'.");

            foreach (string source in sources)
            {
                SyntaxTree tree = ParseTree(sourcePrefix, source);
                IEnumerable<string> forbiddenBooleanNames = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<PropertyDeclarationSyntax>()
                    .Where(property => property.Type is PredefinedTypeSyntax predefined &&
                        predefined.Keyword.IsKind(SyntaxKind.BoolKeyword))
                    .Select(property => property.Identifier.ValueText)
                    .Where(name => IsForbiddenBooleanName(name))
                    .ToArray();

                Assert.That(
                    forbiddenBooleanNames,
                    Is.Empty,
                    $"{source}: boolean names must use Is/Has/Does/Are/Wae/Were forms.");
            }
        }

        private static SyntaxTree ParseTree(string sourcePrefix, string source)
            => CSharpSyntaxTree.ParseText(
                ArchitectureResources.ReadResource(sourcePrefix, source),
                path: source);

        private static bool IsBclTypeName(string name)
            => name is "String" or "Int32" or "Boolean" or "Object" or "Byte" or "Char" or
                "Int64" or "Double" or "Single" or "Decimal" or "UInt32" or "UInt64" or
                "Int16" or "UInt16";

        private static bool IsPascalCase(string name)
            => !string.IsNullOrEmpty(name) && char.IsUpper(name[0]);

        private static bool IsForbiddenBooleanName(string name)
        {
            string[] forbidden =
            [
                "flag",
                "check",
                "result",
                "Flag",
                "Check",
                "Result"
            ];

            return forbidden.Contains(name, StringComparer.Ordinal);
        }
    }
}