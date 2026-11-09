using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using NUnit.Framework;

namespace NuciSecurity.HMAC.ArchitectureTests
{
    [TestFixture]
    public sealed class SolutionConventionTests
    {
        private static string SolutionPath => "NuciSecurity.HMAC.slnx";

        [Test]
        public void GivenSolution_WhenLoaded_ThenItUsesTheSdkStyleFormat()
        {
            XDocument solution = LoadSolution();

            Assert.That(solution.Root, Is.Not.Null, "The solution file must have a root element.");
            Assert.That(
                solution.Root!.Name.LocalName,
                Is.EqualTo("Solution"),
                "The solution file must use the SDK-style <Solution> root element.");

            Assert.That(
                solution.Root.Attributes(),
                Is.Empty,
                "The <Solution> root element must not declare attributes.");

            string content = ArchitectureResources.ReadResource(
                ArchitectureResources.ProjectFilePrefix,
                SolutionPath);

            Assert.That(
                content.TrimStart(),
                Does.Not.StartWith("<?xml"),
                "The solution file must not declare an XML declaration.");
        }

        [Test]
        public void GivenSolution_WhenLoaded_ThenItContainsNoVersionElement()
        {
            XDocument solution = LoadSolution();

            Assert.That(
                solution.Descendants().Where(element =>
                    string.Equals(element.Name.LocalName, "Version", StringComparison.Ordinal)),
                Is.Empty,
                "The solution file must not contain a <Version> element.");
        }

        [Test]
        public void GivenSolution_WhenLoaded_ThenEveryProjectDeclaresOnlyItsPath()
        {
            XDocument solution = LoadSolution();

            IEnumerable<XElement> projects = solution.Descendants()
                .Where(element => string.Equals(element.Name.LocalName, "Project", StringComparison.Ordinal))
                .ToArray();

            Assert.That(projects, Is.Not.Empty, "The solution file must declare projects.");

            foreach (XElement project in projects)
            {
                Assert.That(
                    project.Attributes().Select(attribute => attribute.Name.LocalName),
                    Is.EqualTo(new[] { "Path" }),
                    "Each <Project> element must declare only the Path attribute.");
            }
        }

        [Test]
        public void GivenSolution_WhenLoaded_ThenTheProjectsAreInTheDocumentedOrder()
        {
            XDocument solution = LoadSolution();

            string[] paths = solution.Descendants()
                .Where(element => string.Equals(element.Name.LocalName, "Project", StringComparison.Ordinal))
                .Select(project => GetAttributeValue(project, "Path"))
                .ToArray();

            Assert.That(paths, Is.EqualTo(new[]
            {
                "NuciSecurity.HMAC/NuciSecurity.HMAC.csproj",
                "NuciSecurity.HMAC.ArchitectureTests/NuciSecurity.HMAC.ArchitectureTests.csproj",
                "NuciSecurity.HMAC.UnitTests/NuciSecurity.HMAC.UnitTests.csproj"
            }));
        }

        private static XDocument LoadSolution()
            => XDocument.Parse(
                ArchitectureResources.ReadResource(
                    ArchitectureResources.ProjectFilePrefix,
                    SolutionPath));

        private static string GetAttributeValue(XElement element, string attributeName)
        {
            XAttribute? attribute = element.Attribute(attributeName);

            if (attribute is null)
            {
                return string.Empty;
            }

            return attribute.Value;
        }
    }
}
