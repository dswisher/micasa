// Copyright (c) Doug Swisher. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using Micasa.Cli;
using Micasa.Cli.Drivers;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Micasa.UnitTests
{
    public class ContainerTests
    {
        private readonly IServiceProvider container;

        public ContainerTests()
        {
            // Create the container
            container = Container.CreateContainer();
        }


        [Theory]
        [InlineData("apt")]
        [InlineData("dnf")]
        [InlineData("github-archive")]
        [InlineData("homebrew")]
        [InlineData("shell-script")]
        public void CanResolveDrivers(string driverName)
        {
            // Act
            var driver = container.GetKeyedService<IDriver>(driverName);

            // Assert
            driver.ShouldNotBeNull();
        }
    }
}
