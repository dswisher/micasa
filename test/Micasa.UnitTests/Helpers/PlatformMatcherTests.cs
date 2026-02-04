// Copyright (c) Doug Swisher. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Micasa.Cli.Helpers;
using Xunit;

namespace Micasa.UnitTests.Helpers
{
    public class PlatformMatcherTests
    {
        private readonly PlatformMatcher matcher = new();


        public static IEnumerable<object[]> TestCases => BuildTestCases().Select(x => new object[] { x });


        [Theory]
        [MemberData(nameof(TestCases))]
        public void CanMatchPlatform(PlatformTestCase testCase)
        {
            // Act
            var actualMatch = matcher.FindBestMatch(testCase.Platform, testCase.AvailablePlatforms);

            // Assert
            actualMatch.Should().Be(testCase.ExpectedMatch);
        }


        private static IEnumerable<PlatformTestCase> BuildTestCases()
        {
            yield return new PlatformTestCase
            {
                Platform = "macos-sonoma-arm64",
                AvailablePlatforms = ["macos", "ubuntu-noble"],
                ExpectedMatch = "macos"
            };

            yield return new PlatformTestCase
            {
                Platform = "ubuntu-noble-amd64",
                AvailablePlatforms = ["macos", "ubuntu-noble"],
                ExpectedMatch = "ubuntu-noble"
            };
        }


        public class PlatformTestCase
        {
            public required string Platform { get; init; }
            public required ICollection<string> AvailablePlatforms { get; init; }
            public required string ExpectedMatch { get; init; }
        }
    }
}
