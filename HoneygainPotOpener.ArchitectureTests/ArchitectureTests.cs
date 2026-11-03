using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

using ProductionAssembly = System.Reflection.Assembly;

using ArchUnitNET.Domain;
using ArchUnitNET.Loader;
using ArchUnitNET.NUnit;
using NUnit.Framework;
using NuciWeb.Automation;

using HoneygainPotOpener.Configuration;
using HoneygainPotOpener.Logging;
using HoneygainPotOpener.Processors;
using HoneygainPotOpener.Service;

using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace HoneygainPotOpener.ArchitectureTests
{
    [TestFixture]
    public sealed class DependencyArchitectureTests
    {
        private static readonly Architecture architecture = new ArchLoader()
            .LoadAssemblies(typeof(Program).Assembly, typeof(IWebProcessor).Assembly)
            .Build();

        [Test]
        public void GivenProductionAssembly_WhenLoaded_ThenItContainsExpectedProductionTypes()
        {
            IObjectProvider<IType> productionTypes = Classes().That().ResideInAssembly(typeof(Program).Assembly);

            Assert.That(productionTypes.GetObjects(architecture), Is.Not.Empty);
        }

        [Test]
        public void GivenServiceTypes_WhenChecked_ThenTheyDependOnProcessorContracts()
        {
            Classes().That().Are(typeof(BotService)).Should().DependOnAny(
                Types().That().Are(typeof(IHoneygainProcessor))).Check(architecture);
        }

        [Test]
        public void GivenConcreteProcessor_WhenChecked_ThenItDependsOnWebProcessorContract()
        {
            Classes().That().Are(typeof(HoneygainProcessor)).Should().DependOnAny(
                Types().That().HaveName("IWebProcessor")).Check(architecture);
        }

        [Test]
        public void GivenServiceLayer_WhenChecked_ThenItDoesNotDependOnProcessorImplementation()
        {
            Classes().That().ResideInNamespace(typeof(BotService).Namespace!).Should().NotDependOnAny(
                Types().That().Are(typeof(HoneygainProcessor))).Check(architecture);
        }

        [Test]
        public void GivenConfigurationTypes_WhenChecked_ThenTheyDoNotDependOnServiceTypes()
        {
            Classes().That().ResideInNamespace(typeof(BotSettings).Namespace!).Should().NotDependOnAny(
                Classes().That().ResideInNamespace(typeof(BotService).Namespace!)).Check(architecture);
        }

        [Test]
        public void GivenLoggingTypes_WhenChecked_ThenTheyDoNotDependOnApplicationServices()
        {
            Classes().That().ResideInNamespace(typeof(MyOperation).Namespace!).Should().NotDependOnAny(
                Classes().That().ResideInNamespace(typeof(BotService).Namespace!)).Check(architecture);
        }

        [Test]
        public void GivenProductionTypes_WhenChecked_ThenCompositionIsTheOnlyProgramLayerDependency()
        {
            Classes().That().Are(typeof(Program)).Should().DependOnAny(
                Types().That().Are(typeof(BotService))).Check(architecture);
        }

        [Test]
        public void GivenProductionAssembly_WhenChecked_ThenItContainsNoTestTypes()
        {
            IEnumerable<System.Type> types = typeof(Program).Assembly.GetTypes();

            Assert.That(types.Any(type => type.Namespace?.Contains("Tests", StringComparison.Ordinal) == true), Is.False);
        }
    }

    [TestFixture]
    public sealed class SourceFileConventionTests
    {
        [Test]
        public void GivenProductionSources_WhenChecked_ThenEveryFileContainsOneMatchingDeclaredType()
        {
            ProductionAssembly assembly = typeof(SourceFileConventionTests).Assembly;
            string[] sourceNames = assembly.GetManifestResourceNames()
                .Where(name => name.StartsWith("ProductionSources/", StringComparison.Ordinal))
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();

            Assert.That(sourceNames, Is.Not.Empty);

            foreach (string resourceName in sourceNames)
            {
                string fileName = resourceName["ProductionSources/".Length..];
                string stem = Path.GetFileNameWithoutExtension(fileName);
                string source = ReadResource(assembly, resourceName);
                int declaredTypeCount = CountDeclaredTypes(source);
                string? declaredTypeName = FindDeclaredTypeName(source);

                Assert.That(declaredTypeCount, Is.EqualTo(1), $"{fileName} must contain exactly one declared type.");
                Assert.That(declaredTypeName, Is.EqualTo(stem), $"{fileName} must match its declared type.");
            }
        }

        [Test]
        public void GivenProductionSources_WhenChecked_ThenTheyUseBlockNamespaces()
        {
            ProductionAssembly assembly = typeof(SourceFileConventionTests).Assembly;
            string[] sourceNames = assembly.GetManifestResourceNames()
                .Where(name => name.StartsWith("ProductionSources/", StringComparison.Ordinal))
                .ToArray();

            foreach (string resourceName in sourceNames)
            {
                string source = ReadResource(assembly, resourceName);

                Assert.That(source, Does.Not.Match(@"namespace\s+[^{;]+;"), resourceName);
            }
        }

        [Test]
        public void GivenProductionSources_WhenChecked_ThenTheyDoNotEnableImplicitUsings()
        {
            string projectPath = GetProjectPath("HoneygainPotOpener.csproj");
            XDocument project = XDocument.Load(projectPath);
            XNamespace msbuild = "http://schemas.microsoft.com/developer/msbuild/2003";
            XElement? value = project.Descendants(msbuild + "ImplicitUsings").FirstOrDefault()
                ?? project.Descendants("ImplicitUsings").FirstOrDefault();

            Assert.That(value?.Value, Is.EqualTo("disable"));
        }

        private static int CountDeclaredTypes(string source)
        {
            return source.Split('\n').Count(line => line.Contains(" class ", StringComparison.Ordinal)
                || line.Contains(" interface ", StringComparison.Ordinal)
                || line.Contains(" enum ", StringComparison.Ordinal));
        }

        private static string? FindDeclaredTypeName(string source)
        {
            string[] markers = ["class ", "interface ", "enum "];

            foreach (string line in source.Split('\n'))
            {
                string trimmedLine = line.Trim();
                foreach (string marker in markers)
                {
                    int markerIndex = trimmedLine.IndexOf(marker, StringComparison.Ordinal);
                    if (markerIndex >= 0)
                    {
                        string declaration = trimmedLine[(markerIndex + marker.Length)..];
                        return declaration.Split([' ', ':', '{', '<', '('], StringSplitOptions.RemoveEmptyEntries)[0];
                    }
                }
            }

            return null;
        }

        private static string GetProjectPath(string fileName)
        {
            string projectDirectory = Path.GetDirectoryName(typeof(Program).Assembly.Location)!;
            string projectPath = Path.Combine(projectDirectory, "..", "..", "..", "..", "HoneygainPotOpener", fileName);
            return Path.GetFullPath(projectPath);
        }

        private static string ReadResource(System.Reflection.Assembly assembly, string resourceName)
        {
            using Stream stream = assembly.GetManifestResourceStream(resourceName)!;
            using StreamReader reader = new(stream);
            return reader.ReadToEnd();
        }
    }
}
