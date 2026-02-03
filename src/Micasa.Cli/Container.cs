// Copyright (c) Doug Swisher. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Micasa.Cli.Commands;
using Micasa.Cli.Helpers;
using Micasa.Cli.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Micasa.Cli
{
    public static class Container
    {
        public static ServiceProvider CreateContainer()
        {
            // Create the service collection
            var services = new ServiceCollection();

            // Register MSFT logging
            services.AddLogging(loggingBuilder =>
                loggingBuilder.AddSerilog(dispose: true));

            // Tooling
            services.AddSingleton<IFormulaReader, FormulaReader>();
            services.AddSingleton<IPlatformDecoder, PlatformDecoder>();
            services.AddSingleton<IPlatformMatcher, PlatformMatcher>();

            // Register all the commands
            // TODO - should these be transient, scoped, or singleton?
            services.AddScoped<InstallCommand>();
            services.AddScoped<UninstallCommand>();

            // Build and return the container
            return services.BuildServiceProvider(validateScopes: true);
        }
    }
}
