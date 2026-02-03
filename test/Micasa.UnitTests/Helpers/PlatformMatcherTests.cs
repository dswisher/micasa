// Copyright (c) Doug Swisher. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using FluentAssertions;
using Micasa.Cli.Helpers;
using Xunit;

namespace Micasa.UnitTests.Helpers
{
    public class PlatformMatcherTests
    {
        private readonly PlatformMatcher matcher = new();


        public static IEnumerable<object[]> PlatformTestData =>
            new List<object[]>
            {
                new object[] { "macos-sonoma-arm64", new[] { "macos", "ubuntu-noble" }, "macos" },
                new object[] { "ubuntu-noble-amd64", new[] { "macos", "ubuntu-noble" }, "ubuntu-noble" },
            };


        [Theory]
        [MemberData(nameof(PlatformTestData))]
        public void CanMatchPlatform(string platform, ICollection<string> availablePlatforms, string expectedMatch)
        {
            // Arrange

            // Act
            var actualMatch = matcher.FindBestMatch(platform, availablePlatforms);

            // Assert
            actualMatch.Should().Be(expectedMatch);
        }
    }
}
