// Copyright (c) Doug Swisher. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Micasa.Cli.Commands;
using Micasa.Cli.Helpers;
using Micasa.Cli.Installers;
using Micasa.Cli.Parsers;
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

            // General Tooling
            services.AddSingleton<ICommandRunner, CommandRunner>();
            services.AddSingleton<IFormulaReader, FormulaReader>();
            services.AddSingleton<IPlatformDecoder, PlatformDecoder>();
            services.AddSingleton<IPlatformMatcher, PlatformMatcher>();

            // Register installation drivers with keys matching the Tool string and a factory to resolve them
            services.AddKeyedTransient<IInstallationDriver, HomebrewDriver>("homebrew");
            services.AddKeyedTransient<IInstallationDriver, AdvancedPackageToolDriver>("apt");

            services.AddSingleton<IDriverFactory, DriverFactory>();

            // Parsers for command output
            services.AddSingleton<IHomebrewInfoParser, HomebrewInfoParser>();

            // Register all the commands
            // TODO - should these be transient, scoped, or singleton?
            services.AddScoped<InfoCommand>();
            services.AddScoped<InstallCommand>();
            services.AddScoped<UninstallCommand>();

            // Build and return the container
            return services.BuildServiceProvider(validateScopes: true);
        }
    }
}
