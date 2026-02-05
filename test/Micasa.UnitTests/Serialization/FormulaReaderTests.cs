// Copyright (c) Doug Swisher. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Micasa.Cli.Serialization;
using Shouldly;
using Xunit;

namespace Micasa.UnitTests.Serialization
{
    public class FormulaReaderTests
    {
        private readonly FormulaReader reader = new();

        private readonly CancellationToken token = CancellationToken.None;


        [Fact]
        public async Task CanReadZoxide()
        {
            // Act
            var result = await reader.ReadFormulaAsync("zoxide", token);

            // Assert
            result.ShouldNotBeNull();
            result.Platforms.ShouldContainKey("amazon-2023");
            result.Platforms.ShouldContainKey("ubuntu");
        }
    }
}
