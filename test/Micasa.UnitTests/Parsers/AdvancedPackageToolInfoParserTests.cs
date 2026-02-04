// Copyright (c) Doug Swisher. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Micasa.Cli.Parsers;
using Xunit;

namespace Micasa.UnitTests.Parsers
{
    public class AdvancedPackageToolInfoParserTests
    {
        private static readonly Assembly Assembly = typeof(AdvancedPackageToolInfoParserTests).Assembly;

        private readonly AdvancedPackageToolInfoParser parser = new();

        public static IEnumerable<object[]> TestCases => BuildTestCases().Select(x => new object[] { x });


        [Theory]
        [MemberData(nameof(TestCases))]
        public void CanParseInfo(ParserTestCase testCase)
        {
            // Act
            var details = parser.Parse(testCase.Stdout);

            // Assert
            details.PackageId.Should().Be(testCase.ExpectedPackageId);
            details.StableVersion.Should().Be(testCase.ExpectedStableVersion);
            details.InstalledVersion.Should().Be(testCase.ExpectedInstalledVersion);
        }


        private static IEnumerable<ParserTestCase> BuildTestCases()
        {
            yield return MakeTestCase("apt-cache-installed.txt", "bat", "0.24.0-1build1", "0.24.0-1build1");
            yield return MakeTestCase("apt-cache-not-installed.txt", "bat", "0.24.0-1build1", null);
        }


        private static ParserTestCase MakeTestCase(string fileName, string packageId, string stableVersion, string? installedVersion)
        {
            // Build the path
            var path = $"Micasa.UnitTests.Parsers.TestFiles.{fileName}";

            // Load the content
            string stdout;
            using (var stream = Assembly.GetManifestResourceStream(path))
            {
                if (stream == null)
                {
                    throw new FileNotFoundException($"Could not load resource: {path}");
                }

                using (var reader = new StreamReader(stream))
                {
                    stdout = reader.ReadToEnd();
                }
            }

            return new ParserTestCase
            {
                Stdout = stdout,
                ExpectedPackageId = packageId,
                ExpectedStableVersion = stableVersion,
                ExpectedInstalledVersion = installedVersion
            };
        }


        public class ParserTestCase
        {
            public required string Stdout { get; init; }
            public required string ExpectedPackageId { get; init; }
            public required string ExpectedStableVersion { get; init; }
            public string? ExpectedInstalledVersion { get; init; }
        }
    }
}
