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
    public sealed class CodingConventionTests
    {
        [TestCase(ArchitectureResources.ProductionSourcePrefix)]
        [TestCase(ArchitectureResources.UnitTestSourcePrefix)]
        [TestCase(ArchitectureResources.ArchitectureTestSourcePrefix)]
        public void GivenSources_WhenChecked_ThenExplicitTypesAreUsed(string sourcePrefix)
        {
            IReadOnlyList<string> sources = ArchitectureResources.GetResourcePaths(sourcePrefix);

            Assert.That(sources, Is.Not.Empty, $"No source files were found under '{sourcePrefix}'.");

            foreach (string source in sources)
            {
                SyntaxTree tree = ParseTree(sourcePrefix, source);
                IEnumerable<VariableDeclarationSyntax> varDeclarations = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<VariableDeclarationSyntax>()
                    .Where(declaration => declaration.Type.IsVar)
                    .ToArray();

                Assert.That(
                    varDeclarations,
                    Is.Empty,
                    $"{source}: the 'var' keyword must not be used.");
            }
        }

        [TestCase(ArchitectureResources.ProductionSourcePrefix)]
        [TestCase(ArchitectureResources.UnitTestSourcePrefix)]
        [TestCase(ArchitectureResources.ArchitectureTestSourcePrefix)]
        public void GivenSources_WhenChecked_ThenNoTernariesAreUsed(string sourcePrefix)
        {
            IReadOnlyList<string> sources = ArchitectureResources.GetResourcePaths(sourcePrefix);

            Assert.That(sources, Is.Not.Empty, $"No source files were found under '{sourcePrefix}'.");

            foreach (string source in sources)
            {
                SyntaxTree tree = ParseTree(sourcePrefix, source);
                IEnumerable<ConditionalExpressionSyntax> ternaries = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<ConditionalExpressionSyntax>()
                    .ToArray();

                Assert.That(
                    ternaries,
                    Is.Empty,
                    $"{source}: ternary expressions are not allowed.");
            }
        }

        [TestCase(ArchitectureResources.ProductionSourcePrefix)]
        [TestCase(ArchitectureResources.UnitTestSourcePrefix)]
        [TestCase(ArchitectureResources.ArchitectureTestSourcePrefix)]
        public void GivenSources_WhenChecked_ThenNoNullCoalescingOperatorIsUsed(string sourcePrefix)
        {
            IReadOnlyList<string> sources = ArchitectureResources.GetResourcePaths(sourcePrefix);

            Assert.That(sources, Is.Not.Empty, $"No source files were found under '{sourcePrefix}'.");

            foreach (string source in sources)
            {
                SyntaxTree tree = ParseTree(sourcePrefix, source);
                IEnumerable<BinaryExpressionSyntax> coalescing = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<BinaryExpressionSyntax>()
                    .Where(expression => expression.IsKind(SyntaxKind.CoalesceExpression))
                    .ToArray();

                Assert.That(
                    coalescing,
                    Is.Empty,
                    $"{source}: the null-coalescing operator '??' is not allowed.");
            }
        }

        [TestCase(ArchitectureResources.ProductionSourcePrefix)]
        [TestCase(ArchitectureResources.UnitTestSourcePrefix)]
        [TestCase(ArchitectureResources.ArchitectureTestSourcePrefix)]
        public void GivenSources_WhenChecked_ThenNoTuplesAreUsed(string sourcePrefix)
        {
            IReadOnlyList<string> sources = ArchitectureResources.GetResourcePaths(sourcePrefix);

            Assert.That(sources, Is.Not.Empty, $"No source files were found under '{sourcePrefix}'.");

            foreach (string source in sources)
            {
                SyntaxTree tree = ParseTree(sourcePrefix, source);
                IEnumerable<SyntaxNode> tuples = tree.GetRoot()
                    .DescendantNodes()
                    .Where(node => node.IsKind(SyntaxKind.TupleType) || node.IsKind(SyntaxKind.TupleExpression))
                    .ToArray();

                Assert.That(
                    tuples,
                    Is.Empty,
                    $"{source}: tuples are not allowed.");
            }
        }

        [TestCase(ArchitectureResources.ProductionSourcePrefix)]
        [TestCase(ArchitectureResources.UnitTestSourcePrefix)]
        [TestCase(ArchitectureResources.ArchitectureTestSourcePrefix)]
        public void GivenSources_WhenChecked_ThenNoRegionsAreUsed(string sourcePrefix)
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

        [TestCase(ArchitectureResources.ProductionSourcePrefix)]
        [TestCase(ArchitectureResources.UnitTestSourcePrefix)]
        [TestCase(ArchitectureResources.ArchitectureTestSourcePrefix)]
        public void GivenSources_WhenChecked_ThenNoNullableDirectivesAreUsed(string sourcePrefix)
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
        public void GivenSources_WhenChecked_ThenEveryControlFlowBodyUsesBraces(string sourcePrefix)
        {
            IReadOnlyList<string> sources = ArchitectureResources.GetResourcePaths(sourcePrefix);

            Assert.That(sources, Is.Not.Empty, $"No source files were found under '{sourcePrefix}'.");

            foreach (string source in sources)
            {
                SyntaxTree tree = ParseTree(sourcePrefix, source);
                IEnumerable<SyntaxNode> unbraced = tree.GetRoot()
                    .DescendantNodes()
                    .Where(node => node is IfStatementSyntax ifStatement && ifStatement.Statement is not BlockSyntax)
                    .ToArray();

                Assert.That(
                    unbraced,
                    Is.Empty,
                    $"{source}: every control-flow body must use braces.");
            }
        }

        [TestCase(ArchitectureResources.ProductionSourcePrefix)]
        [TestCase(ArchitectureResources.UnitTestSourcePrefix)]
        [TestCase(ArchitectureResources.ArchitectureTestSourcePrefix)]
        public void GivenSources_WhenChecked_ThenEveryMethodHasAtMostSevenParameters(string sourcePrefix)
        {
            IReadOnlyList<string> sources = ArchitectureResources.GetResourcePaths(sourcePrefix);

            Assert.That(sources, Is.Not.Empty, $"No source files were found under '{sourcePrefix}'.");

            foreach (string source in sources)
            {
                SyntaxTree tree = ParseTree(sourcePrefix, source);
                IEnumerable<SyntaxNode> tooManyParameters = tree.GetRoot()
                    .DescendantNodes()
                    .Where(node => node is MethodDeclarationSyntax method && method.ParameterList.Parameters.Count > 7)
                    .ToArray();

                Assert.That(
                    tooManyParameters,
                    Is.Empty,
                    $"{source}: methods must have at most seven parameters.");
            }
        }

        [TestCase(ArchitectureResources.ProductionSourcePrefix)]
        [TestCase(ArchitectureResources.UnitTestSourcePrefix)]
        [TestCase(ArchitectureResources.ArchitectureTestSourcePrefix)]
        public void GivenSources_WhenChecked_ThenNoRecordTypesAreUsed(string sourcePrefix)
        {
            IReadOnlyList<string> sources = ArchitectureResources.GetResourcePaths(sourcePrefix);

            Assert.That(sources, Is.Not.Empty, $"No source files were found under '{sourcePrefix}'.");

            foreach (string source in sources)
            {
                SyntaxTree tree = ParseTree(sourcePrefix, source);
                IEnumerable<SyntaxNode> recordDeclarations = tree.GetRoot()
                    .DescendantNodes()
                    .Where(node => node is RecordDeclarationSyntax)
                    .ToArray();

                Assert.That(
                    recordDeclarations,
                    Is.Empty,
                    $"{source}: record types are not allowed.");
            }
        }

        [TestCase(ArchitectureResources.ProductionSourcePrefix)]
        [TestCase(ArchitectureResources.UnitTestSourcePrefix)]
        [TestCase(ArchitectureResources.ArchitectureTestSourcePrefix)]
        public void GivenSources_WhenChecked_ThenNoTopLevelStatementsAreUsed(string sourcePrefix)
        {
            IReadOnlyList<string> sources = ArchitectureResources.GetResourcePaths(sourcePrefix);

            Assert.That(sources, Is.Not.Empty, $"No source files were found under '{sourcePrefix}'.");

            foreach (string source in sources)
            {
                SyntaxTree tree = ParseTree(sourcePrefix, source);
                IEnumerable<SyntaxNode> topLevelStatements = tree.GetRoot()
                    .DescendantNodes()
                    .Where(node => node is GlobalStatementSyntax)
                    .ToArray();

                Assert.That(
                    topLevelStatements,
                    Is.Empty,
                    $"{source}: top-level statements are not allowed.");
            }
        }

        [TestCase(ArchitectureResources.ProductionSourcePrefix)]
        [TestCase(ArchitectureResources.UnitTestSourcePrefix)]
        [TestCase(ArchitectureResources.ArchitectureTestSourcePrefix)]
        public void GivenSources_WhenChecked_ThenNoRefParametersAreUsed(string sourcePrefix)
        {
            IReadOnlyList<string> sources = ArchitectureResources.GetResourcePaths(sourcePrefix);

            Assert.That(sources, Is.Not.Empty, $"No source files were found under '{sourcePrefix}'.");

            foreach (string source in sources)
            {
                SyntaxTree tree = ParseTree(sourcePrefix, source);
                IEnumerable<SyntaxNode> refParameters = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<ParameterSyntax>()
                    .Where(parameter => parameter.Modifiers.Any(modifier =>
                        modifier.IsKind(SyntaxKind.RefKeyword) ||
                        modifier.IsKind(SyntaxKind.OutKeyword)))
                    .ToArray();

                Assert.That(
                    refParameters,
                    Is.Empty,
                    $"{source}: ref/out parameters are not allowed.");
            }
        }

        [TestCase(ArchitectureResources.ProductionSourcePrefix)]
        [TestCase(ArchitectureResources.UnitTestSourcePrefix)]
        [TestCase(ArchitectureResources.ArchitectureTestSourcePrefix)]
        public void GivenSources_WhenChecked_ThenNoOptionalParametersAreUsed(string sourcePrefix)
        {
            IReadOnlyList<string> sources = ArchitectureResources.GetResourcePaths(sourcePrefix);

            Assert.That(sources, Is.Not.Empty, $"No source files were found under '{sourcePrefix}'.");

            foreach (string source in sources)
            {
                SyntaxTree tree = ParseTree(sourcePrefix, source);
                IEnumerable<SyntaxNode> optionalParameters = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<ParameterSyntax>()
                    .Where(parameter => parameter.Default is not null)
                    .ToArray();

                Assert.That(
                    optionalParameters,
                    Is.Empty,
                    $"{source}: optional parameters are not allowed.");
            }
        }

        [TestCase(ArchitectureResources.ProductionSourcePrefix)]
        public void GivenProductionSources_WhenChecked_ThenEveryPublicMemberHasXmlDocumentation(string sourcePrefix)
        {
            IReadOnlyList<string> sources = ArchitectureResources.GetResourcePaths(sourcePrefix);

            Assert.That(sources, Is.Not.Empty, $"No source files were found under '{sourcePrefix}'.");

            foreach (string source in sources)
            {
                SyntaxTree tree = ParseTree(sourcePrefix, source);
                IEnumerable<SyntaxNode> undocumented = tree.GetRoot()
                    .DescendantNodes()
                    .Where(node => node is MethodDeclarationSyntax method &&
                            method.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.PublicKeyword)) &&
                            !HasDocumentation(method))
                    .ToArray();

                Assert.That(
                    undocumented,
                    Is.Empty,
                    $"{source}: every public member must have XML documentation.");
            }
        }

        [TestCase(ArchitectureResources.ProductionSourcePrefix)]
        public void GivenProductionSources_WhenChecked_ThenNoInlineFullyQualifiedTypesAreUsed(string sourcePrefix)
        {
            IReadOnlyList<string> sources = ArchitectureResources.GetResourcePaths(sourcePrefix);

            Assert.That(sources, Is.Not.Empty, $"No source files were found under '{sourcePrefix}'.");

            foreach (string source in sources)
            {
                string content = ArchitectureResources.ReadResource(sourcePrefix, source);

                Assert.That(
                    content,
                    Does.Not.Match(@"\bSystem\.(String|Int32|Boolean|Object|DateTime|Exception)\b"),
                    $"{source}: inline fully qualified BCL types are not allowed.");
            }
        }

        private static SyntaxTree ParseTree(string sourcePrefix, string source)
            => CSharpSyntaxTree.ParseText(
                ArchitectureResources.ReadResource(sourcePrefix, source),
                path: source);

        private static bool HasDocumentation(MemberDeclarationSyntax member)
            => member.GetLeadingTrivia().Any(trivia =>
                trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) ||
                trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia));
    }
}
