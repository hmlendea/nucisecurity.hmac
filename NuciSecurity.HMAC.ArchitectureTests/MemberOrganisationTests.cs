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
    public sealed class MemberOrganisationTests
    {
        [TestCase(ArchitectureResources.ProductionSourcePrefix)]
        [TestCase(ArchitectureResources.UnitTestSourcePrefix)]
        [TestCase(ArchitectureResources.ArchitectureTestSourcePrefix)]
        public void GivenSources_WhenChecked_ThenEveryMemberDeclaresExplicitAccessibility(string sourcePrefix)
        {
            IReadOnlyList<string> sources = ArchitectureResources.GetResourcePaths(sourcePrefix);

            Assert.That(sources, Is.Not.Empty, $"No source files were found under '{sourcePrefix}'.");

            foreach (string source in sources)
            {
                SyntaxTree tree = ParseTree(sourcePrefix, source);
                IEnumerable<SyntaxNode> membersWithoutAccessibility = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<MemberDeclarationSyntax>()
                    .Where(IsFieldOrMethodOrPropertyOrConstructor)
                    .Where(member => !DeclaresAccessibility(member))
                    .ToArray();

                Assert.That(
                    membersWithoutAccessibility,
                    Is.Empty,
                    $"{source}: every field, property, event, constructor, and method must declare accessibility.");
            }
        }

        [TestCase(ArchitectureResources.ProductionSourcePrefix)]
        [TestCase(ArchitectureResources.UnitTestSourcePrefix)]
        [TestCase(ArchitectureResources.ArchitectureTestSourcePrefix)]
        public void GivenSources_WhenChecked_ThenMembersAreOrderedByKind(string sourcePrefix)
        {
            IReadOnlyList<string> sources = ArchitectureResources.GetResourcePaths(sourcePrefix);

            Assert.That(sources, Is.Not.Empty, $"No source files were found under '{sourcePrefix}'.");

            foreach (string source in sources)
            {
                SyntaxTree tree = ParseTree(sourcePrefix, source);

                foreach (TypeDeclarationSyntax typeDeclaration in tree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>())
                {
                    MemberDeclarationSyntax[] members = typeDeclaration.Members.ToArray();

                    int[] kinds = members.Select(GetMemberKindOrder).Where(order => order >= 0).ToArray();

                    Assert.That(
                        kinds,
                        Is.Ordered,
                        $"{source}: members of '{typeDeclaration.Identifier.ValueText}' must be ordered fields -> properties -> events -> constructors -> methods.");
                }
            }
        }

        [TestCase(ArchitectureResources.ProductionSourcePrefix)]
        [TestCase(ArchitectureResources.UnitTestSourcePrefix)]
        [TestCase(ArchitectureResources.ArchitectureTestSourcePrefix)]
        public void GivenSources_WhenChecked_ThenNoBlankLinePrecedesClosingDelimiters(string sourcePrefix)
        {
            IReadOnlyList<string> sources = ArchitectureResources.GetResourcePaths(sourcePrefix);

            Assert.That(sources, Is.Not.Empty, $"No source files were found under '{sourcePrefix}'.");

            foreach (string source in sources)
            {
                string content = ArchitectureResources.ReadResource(sourcePrefix, source);

                Assert.That(
                    content,
                    Does.Not.Match(@"\n[ \t]*\n[ \t]*[}\])]"),
                    $"{source}: no blank line may precede a closing delimiter.");
            }
        }

        [TestCase(ArchitectureResources.ProductionSourcePrefix)]
        [TestCase(ArchitectureResources.UnitTestSourcePrefix)]
        [TestCase(ArchitectureResources.ArchitectureTestSourcePrefix)]
        public void GivenSources_WhenChecked_ThenNoTrailingWhitespaceExists(string sourcePrefix)
        {
            IReadOnlyList<string> sources = ArchitectureResources.GetResourcePaths(sourcePrefix);

            Assert.That(sources, Is.Not.Empty, $"No source files were found under '{sourcePrefix}'.");

            foreach (string source in sources)
            {
                string content = ArchitectureResources.ReadResource(sourcePrefix, source);

                Assert.That(
                    content,
                    Does.Not.Match(@"[ \t]+(\r?\n)"),
                    $"{source}: trailing whitespace is not allowed.");
            }
        }

        [TestCase(ArchitectureResources.ProductionSourcePrefix)]
        [TestCase(ArchitectureResources.UnitTestSourcePrefix)]
        [TestCase(ArchitectureResources.ArchitectureTestSourcePrefix)]
        public void GivenSources_WhenChecked_ThenUsingDirectivesAreAlphabetisedWithinGroups(string sourcePrefix)
        {
            IReadOnlyList<string> sources = ArchitectureResources.GetResourcePaths(sourcePrefix);

            Assert.That(sources, Is.Not.Empty, $"No source files were found under '{sourcePrefix}'.");

            foreach (string source in sources)
            {
                string[] lines = ArchitectureResources.ReadResource(sourcePrefix, source)
                    .Split('\n')
                    .Select(line => line.TrimEnd('\r'))
                    .ToArray();

                List<List<string>> groups = new();
                List<string> currentGroup = new();

                foreach (string line in lines)
                {
                    if (line.StartsWith("using ", StringComparison.Ordinal) && line.EndsWith(';'))
                    {
                        currentGroup.Add(line);
                        continue;
                    }

                    if (currentGroup.Count > 0)
                    {
                        groups.Add(currentGroup);
                        currentGroup = [];
                    }
                }

                if (currentGroup.Count > 0)
                {
                    groups.Add(currentGroup);
                }

                foreach (List<string> group in groups)
                {
                    List<string> sortedGroup = group
                        .OrderBy(GetUsingDirectiveName, StringComparer.Ordinal)
                        .ToList();

                    Assert.That(
                        group.Select(GetUsingDirectiveName),
                        Is.EqualTo(sortedGroup.Select(GetUsingDirectiveName)),
                        $"{source}: using directives must be alphabetised within each group.");
                }
            }
        }

        [TestCase(ArchitectureResources.ProductionSourcePrefix)]
        [TestCase(ArchitectureResources.UnitTestSourcePrefix)]
        [TestCase(ArchitectureResources.ArchitectureTestSourcePrefix)]
        public void GivenSources_WhenChecked_ThenNoTrailingBlankLineBeforeNamespace(string sourcePrefix)
        {
            IReadOnlyList<string> sources = ArchitectureResources.GetResourcePaths(sourcePrefix);

            Assert.That(sources, Is.Not.Empty, $"No source files were found under '{sourcePrefix}'.");

            foreach (string source in sources)
            {
                string content = ArchitectureResources.ReadResource(sourcePrefix, source);

                Assert.That(
                    content,
                    Does.Not.Match(@"\n[ \t]*\n[ \t]*\n[ \t]*namespace "),
                    $"{source}: exactly one blank line must precede the namespace declaration.");
            }
        }

        private static SyntaxTree ParseTree(string sourcePrefix, string source)
            => CSharpSyntaxTree.ParseText(
                ArchitectureResources.ReadResource(sourcePrefix, source),
                path: source);

        private static string GetUsingDirectiveName(string usingDirective)
            => usingDirective.Replace("using ", string.Empty, StringComparison.Ordinal).TrimEnd(';');

        private static bool IsFieldOrMethodOrPropertyOrConstructor(MemberDeclarationSyntax member)
            => member is FieldDeclarationSyntax ||
                member is PropertyDeclarationSyntax ||
                member is EventFieldDeclarationSyntax ||
                member is ConstructorDeclarationSyntax ||
                member is MethodDeclarationSyntax;

        private static bool DeclaresAccessibility(MemberDeclarationSyntax member)
        {
            SyntaxTokenList modifiers = member switch
            {
                FieldDeclarationSyntax field => field.Modifiers,
                PropertyDeclarationSyntax property => property.Modifiers,
                EventFieldDeclarationSyntax eventField => eventField.Modifiers,
                ConstructorDeclarationSyntax constructor => constructor.Modifiers,
                MethodDeclarationSyntax method => method.Modifiers,
                _ => default
            };

            return modifiers.Any(modifier =>
                modifier.IsKind(SyntaxKind.PublicKeyword) ||
                modifier.IsKind(SyntaxKind.ProtectedKeyword) ||
                modifier.IsKind(SyntaxKind.InternalKeyword) ||
                modifier.IsKind(SyntaxKind.PrivateKeyword));
        }

        private static int GetMemberKindOrder(MemberDeclarationSyntax member)
        {
            if (member is FieldDeclarationSyntax)
            {
                return 0;
            }

            if (member is PropertyDeclarationSyntax)
            {
                return 1;
            }

            if (member is EventFieldDeclarationSyntax)
            {
                return 2;
            }

            if (member is ConstructorDeclarationSyntax)
            {
                return 3;
            }

            if (member is MethodDeclarationSyntax)
            {
                return 4;
            }

            return -1;
        }
    }
}